using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;


class Program
{
    static string returned = null;                      //the output
    static int COUNTER = 0, found = 0, DELTA_COUNTER=0; //counters to know when all the thread in the threadpool are done
    static string readed_text;                          //the given text insert to global variable

    public static int Main(string[] args)
    {
        int threads = Convert.ToInt32(args[2]);
        int delta = Convert.ToInt32(args[3]);
        run(args[0], args[1], threads, delta);
        return 0;
    }


    //this search make with loops, run in delta sizes
    public static void search_in_delta(string StringToSearch, int Delta, int starting_index)
    {
        //takes text and search for the string inside.
        for (int i = 0; i < readed_text.Length; i++)
        {
            if (readed_text[i] == StringToSearch[0])
            {
                int k = i;
                for (int j = 0; j < StringToSearch.Length; j++, k += Delta + 1)
                {
                    if (k > readed_text.Length - 1 || StringToSearch[j] != readed_text[k])
                        break;
                    if (j == StringToSearch.Length - 1 && found == 0)
                    {
                        Interlocked.Increment(ref found);
                        returned = i.ToString();
                        break;
                    }
                }
                if (found == 1)
                    break;
            }
        }
        Interlocked.Increment(ref DELTA_COUNTER);
    }

    //this search take buffer and search in it
    public static void search_in_sector(string text, string StringToSearch, int Delta, int starting_index)
    {
        //takes text and search for the string inside.
        for (int i = 0; i < text.Length && found == 0; i++)
        {
            if (text[i] == StringToSearch[0])
            {
                int k = i;
                for (int j = 0; j < StringToSearch.Length; j++, k += Delta + 1)
                {
                    if (k > text.Length - 1 || StringToSearch[j] != text[k])
                        break;
                    if (j == StringToSearch.Length - 1 && found == 0)
                    {
                        Interlocked.Increment(ref found);
                        returned = (starting_index + i).ToString();
                        break;
                    }
                }
            }
        }
        Interlocked.Increment(ref COUNTER);
    }

    //run the program
    public static void run(string textfile, string StringToSearch, int nThreads, int Delta)
    {
        readed_text = File.ReadAllText(@textfile);
        if (readed_text.Length == 0)
        {
            Console.WriteLine(returned ?? "not found");
            return;
        }

        //thread pool
        int thread_long = (int)Math.Ceiling((decimal)(readed_text.Length / nThreads));      //how long the text we will sent to the thread
        int runs = (int)Math.Ceiling((decimal)readed_text.Length / thread_long)-1;          //number of times we need the threads run on the text
        thread_long = thread_long > 10000 ? 10000 - 2 * Delta : thread_long;    //add delta to the text to check if the word between the slices
        ThreadPool.SetMaxThreads(nThreads, 0);

        //sent the text and every thing the function needs to search inside it
        for (int i = 0; i < runs; i++)
        {
            int start = 0, end = 0;
            start = i * thread_long;
            start = start - Delta < 0 ? 0 : start - Delta;
            end = thread_long * (i + 1);
            end = end + Delta > readed_text.Length ? readed_text.Length - 1 : end + Delta;
            if (end == start)
                break;
            string s_buffer = readed_text[start..end];
            ThreadPool.QueueUserWorkItem((o) => search_in_sector(s_buffer, StringToSearch, Delta, start));
        }
        while (COUNTER != runs);

        if (returned == null)
        {
            int i;
            for(i=0; i <= Delta; i++)
                ThreadPool.QueueUserWorkItem((o) => search_in_delta(StringToSearch, Delta, i));
            if(Delta==0)
                while (DELTA_COUNTER != 1);
            else
                while (DELTA_COUNTER != i);
        }


        Console.WriteLine(returned ?? "not found");
    }
}
