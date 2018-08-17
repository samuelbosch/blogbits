using System;
using System.Collections.Generic;
using System.Linq;

namespace multab
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write(Messages.WELCOME);
            while (true) {
                var input = Console.ReadLine().ToLower().Trim();
                var parts = input.Split(' ');
                var command = parts[0];
                switch(command) {
                    case "exit":
                        return;
                    case "help":
                        Console.Write(Messages.HELP);
                        break;
                    case "print":
                        var number = GetNumberFromInput(parts);
                        if(number != null) {
                            PrintTable(number.Value);
                        } else {
                            Console.Write(Messages.PREFIX);
                        }
                        break;
                    case "table":
                        number = GetNumberFromInput(parts);
                        if(number != null) {
                            ExerciseTable(number.Value);
                            Console.Write("\n" + Messages.PREFIX);
                        } else {
                            Console.Write(Messages.PREFIX);
                        }
                        break;
                    default:
                        Console.Write(Messages.WRONGCOMMAND);
                        break;
                }
            }
        }

        static int? GetNumberFromInput(string[] inputParts) {
            int number;
            if(Int32.TryParse(inputParts[inputParts.Length-1], out number) && number >= 0) {
                return number;
            } else {
                Console.WriteLine("Number not recognized");
                return null;
            }
            
        }

        static void PrintTable(int number) {
            for(int i = 1; i <= 10; i++) {
                Console.WriteLine("{0} x {1} = {2}", i.ToString().PadLeft(2), number, i*number);
            }
            Console.Write(Messages.PREFIX);
        } 

        static void ExerciseTable(int number) {
            Console.WriteLine("Type the correct answer and hit <ENTER>, to stop type stop and hit <ENTER>");
            var multiplicants = GenerateMultiplicants();
            var x = multiplicants.Dequeue();
            int attempts = 0;
            int? answer = null;
            while(true) {
                Console.Write("{0} x {1} = ", x.ToString().PadLeft(2), number);
                var input = Console.ReadLine().Trim().ToLower();
                if(input == "stop") {
                    return;
                }
                answer = GetNumberFromInput(new string[] {input});
                if((answer.HasValue && answer.Value == x*number) || ++attempts > 2) {
                    if(attempts > 2) {
                        Console.WriteLine("Keep going, you'll learn it :-)");
                    }
                    attempts = 0;
                    multiplicants.Enqueue(x); // re-enqueue the previous multiplicant before dequeueing a new one
                    x = multiplicants.Dequeue();
                } else {
                    Console.WriteLine("Try again:");
                }
            }
        }

        static Queue<int> GenerateMultiplicants() {
            // store 10 shuffled arrays consecutively in a queue
            var r = new Random(42);
            var multiplicants = new Queue<int>(100);
            int last = int.MinValue;
            for(int i = 0; i < 10; i++) {
                var shuffledOneToTen = Enumerable.Range(1, 10).ToArray().Shuffle(r);
                if(last == shuffledOneToTen[0]) {
                    // swap first with last to avoid repeting elements
                    shuffledOneToTen[0] = shuffledOneToTen.Last();
                    shuffledOneToTen[shuffledOneToTen.Length+1] = last;
                }
                Array.ForEach(shuffledOneToTen, multiplicants.Enqueue);
                last = shuffledOneToTen.Last();
            }
            return multiplicants;
        }
    }

    internal class Messages
    {
        internal const string WELCOME = @"Hi,
Welcome to multab!
What would you like to do?
" + HELP;
        internal const string PREFIX = "multab> ";
        internal const string HELP = @"- To print a multiplication table type up to 10 for e.g. the number 5 type (and hit <ENTER>): 
multab> print 5
- To train yourself for the multiplication of e.g. the number 5 type (and hit <ENTER>):
multab> table 5
- For help type (and hit <ENTER>):
multab> help
- To exit type (and hit <ENTER>):
multab> exit

" + PREFIX; 
        internal const string WRONGCOMMAND = @"Not clear what you want to do!?
" + HELP;
    }
    
    public static class ArrayExtensions
    {
        // Fisher Yates: shuffle extension method, adapted from http://rextester.com/DUXNT12340
        public static T[] Shuffle<T>(this T[] array, Random random)
        {
            int n = array.Length;
            for (int i = 0; i < n; i++)
            {
                int r = i + random.Next(n - i);
                T t = array[r];
                array[r] = array[i];
                array[i] = t;
            }
            return array;
        }
    }
}
