using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using System.Linq;

namespace WordleSolver
{
    class Program
    {
        static void Main(string[] args)
        {
            var gameNum = 212;

            try { gameNum = int.Parse(args[0]); } catch(Exception) { }

            Console.WriteLine($"Hello Wordle {gameNum}!");
            var solver = new WordleSolver();
            solver.Solve(gameNum);
        }
    }

    class WordleSolver
    {
        private string[] words { get; }
        private int compares = 0;
        public WordleSolver()
        {
            words = LoadWordList(@"sgb-words.txt");
        }

        public void Solve(int gameNum)
        {
            var patterns = GetPatternsFromTwitter(gameNum);
            var validWords = new List<string>();
            validWords.AddRange(words);
            var toRemove = new List<string>();
            var iteration = 0;
            compares = 0;
            var accumPatterns = new List<string>();

            foreach (var pattern in patterns)
            {
                toRemove.Clear();
                foreach (var candidateWord in validWords)
                {
                    if (!TestCandidate(pattern.Key, words, candidateWord))
                    {
                        toRemove.Add(candidateWord);
                        if (candidateWord == "prick")
                        {
                            Console.WriteLine($"Found answer.");
                        }
                    }
                }
                foreach (var invalidWord in toRemove) 
                {
                   validWords.Remove(invalidWord);
                }

                Console.WriteLine($"{++iteration}. Using pattern {pattern} excluded {toRemove.Count} words. Remaining word count: {validWords.Count}.");
            }

            Console.WriteLine($"\nDone.");
            Console.WriteLine($"Number of compares: {compares:N0}.");
        }

        private IEnumerable<(string Key, int Score, int Count)> GetPatternsFromTwitter(int index)
        {
            static IEnumerable<(string Key, int Score, int Value)> ResultsSorter(Dictionary<string, int> patterns, int appearanceThreshold)
            {
                return from p in patterns where p.Value > appearanceThreshold orderby PatternScore(p.Key) descending, p.Value descending select (p.Key, PatternScore(p.Key), p.Value);
            }

            var userClient = new TwitterClient(
                "GElfTtVcx5f1DVPgKsTz4MFoh", // Consumer Key
                "QSrkZ6u9smAfC9d4KyaXBLkOuOH4p6DyzcD8ZgKKNI9mksZuA4", // Consumer Secret
                "AAAAAAAAAAAAAAAAAAAAAA4hYQEAAAAAfsJrYryFiQgRWdbYTKS8p70Pths%3D9ldmJv9Fxth0HCd3TnVcuT0o2mNPVBa4Hjk6jslpBylUf4N5gQ" // Access Token
                // "ACCESS_TOKEN_SECRET" // Access Token Secret
            ); 

            var patterns = new Dictionary<string, int>();
            var appearanceThreshold = 1;
            var expectedPatterns = 120;
            var tweetCount = 0;

            var res = userClient.Search.GetSearchTweetsIterator(new SearchTweetsParameters($"Wordle {index}"));
            try
            {
                while (!res.Completed)
                {
                    var page = res.NextPageAsync(); 
                    var tweets = new List<ITweet>();
                    tweets.AddRange(page.Result);

                    foreach (var tweet in tweets)
                    {
                        if (tweet.FullText.StartsWith($"Wordle {index}"))
                        {
                            tweetCount++;
                            var lines = tweet.FullText.Split("\n");
                            bool start = false;
                            foreach (var line in lines)
                            {
                                // "Wordle 212 5/6\n\n⬛🟨⬛⬛🟨\n⬛🟨⬛⬛🟨\n🟩🟩🟨🟨⬛\n⬛⬛⬛⬛🟨\n🟩🟩🟩🟩🟩"
                                // "Wordle 212 6/6\n\n⬜⬜⬜⬜🟩\n⬜🟨🟩⬜🟩\n🟨⬜🟩🟨🟩\n🟩⬜🟩🟩🟩\n🟩⬜🟩🟩🟩\n🟩🟩🟩🟩🟩"
                                // "Wordle 212 3/6\n\n⬛🟦⬛🟦🟦\n⬛🟧🟦🟦🟦\n🟧🟧🟧🟧🟧"
                                // "Wordle 212 5/6\n\n⬜️⬜️⬜️🟨⬜️\n⬜️⬜️🟩⬜️⬜️\n⬜️🟨🟨⬜️🟨\n🟨🟨🟩🟩🟨\n🟩🟩🟩🟩🟩"

                                if (line.Trim().Length == 0)
                                {
                                    continue;
                                }
                                if (line.Contains("🟩") || line.Contains("🟧"))
                                {
                                    start = true;
                                    var pattern = line.
                                        Replace("🟩", "+").Replace("🟧", "+").
                                        Replace("🟨", "?").Replace("🟦", "?").
                                        Replace("⬛", "-").Replace("⬜", "-").Replace("⬜️", "-");

                                    // Clean extra garbage
                                    Regex reg_exp = new Regex("[^\\-+\\?]+");
                                    pattern = reg_exp.Replace(pattern, "");

                                    // Verify pattern is useful and valid
                                    if (pattern != "+++++" && pattern.Length == 5) 
                                    {
                                        var testPattern = pattern.Replace("+", "").Replace("-", "").Replace("?", "");
                                        if (testPattern == "")
                                        {
                                            var appearanceCount = 0;
                                            patterns.TryGetValue(pattern, out appearanceCount);
                                            patterns[pattern] = appearanceCount + 1;
                                            if (appearanceCount == appearanceThreshold)
                                            {
                                                expectedPatterns--;
                                                if (expectedPatterns == 0)
                                                {
                                                  Console.WriteLine();
                                                  return ResultsSorter(patterns, appearanceThreshold);
                                                }
                                            }
                                            Console.Write($"\rTweets processed: {tweetCount}. Still looking for {expectedPatterns} good patterns... ");
                                        }
                                    }
                                } 
                                else 
                                {
                                    if (start)
                                        // Ignore the rest of the tweet if already found a pattern (some tweets included more instances)
                                        break;
                                }
                            }
                        }
                    }
                } 
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            Console.WriteLine(" Couldn't satify the threshold.");
            return ResultsSorter(patterns, appearanceThreshold);

    }

        private string[] LoadWordList(string Filename)
        {
            string[] words = System.IO.File.ReadAllText(Filename).Split('\n');
            return words;
        }        

        private static int PatternScore(string pattern)
        {
            var score = 0;
            for (int i = 0; i < 5; i++)
            {
                if (pattern[i] == '+')
                {
                    score = score + 3;
                }
                else if (pattern[i] == '?')
                {
                    score = score + 2;
                }
                else if (pattern[i] == '-')
                {
                    score = score - 2;
                }

            }
            return score;
        }

        private bool PatternMatch(string pattern, string candidate, string test)
        {
            if (pattern.Length != 5 || candidate.Length != 5 || test.Length != 5)
            {
                throw new Exception("Invalid input");
            }

            compares++;
            for (int i = 0; i < 5; i++)
            { 
                switch (pattern[i])
                {
                    case '+': // Letter must match
                        if (candidate[i] != test[i])
                        {
                            return false;
                        }
                        break;
                    case '-': // Letter must NOT match
                        if (candidate[i] == test[i])
                        {
                            return false;
                        }
                        break;
                    case '?': // Letter must match any letter in ANOTHER position
                        if (candidate[i] == test[i])
                        {
                            return false;
                        }
                        if (!candidate.Contains(test[i]))
                        {
                            return false;
                        }
                        break;
                }
            }
            return true;
        }

        private bool TestCandidate(string pattern, string[] words, string candidate)
        {
            foreach (var testWord in words)
            {
                if (candidate == testWord)
                {
                    continue;
                }

                if (PatternMatch(pattern, candidate, testWord))
                {
                    // Need to find just one example that matches. 
                    return true;
                }
            }
            return false;
        }
    }
}
