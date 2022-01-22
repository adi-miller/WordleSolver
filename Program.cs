using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Tweetinvi;
using Tweetinvi.Models;
using System.Linq;

namespace WordleSolver
{
    class Program
    {
        static void Main(string[] args)
        {
            var gameNum = 210;
            try { gameNum = int.Parse(args[0]); } catch(Exception) { }

            Console.WriteLine($"Hello Wordle {gameNum}!");

            var solver = new WordleSolver();
            solver.Solve(gameNum);
        }
    }

    class WordleSolver
    {
        const int frequencyThreshold = 5; // Recommended: 5. To avoid fake results and non-English tweets. 
        const int scoreThreshold = 8; // Recommended: 8. Max score = 14 [e.g. +++?+]
        const int expectedPatterns = 15; // Number of patterns above thresholds. 
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

            foreach (var pattern in patterns)
            {
                toRemove.Clear();
                foreach (var candidateWord in validWords)
                {
                    if (!TestCandidate(pattern.Key, words, candidateWord))
                    {
                        toRemove.Add(candidateWord);
                    }
                }
                foreach (var invalidWord in toRemove) 
                {
                   validWords.Remove(invalidWord);
                }

                Console.WriteLine($"\r{++iteration}. Using pattern {pattern} excluded {toRemove.Count} words. Remaining word count: {validWords.Count}.");
            }

            Console.WriteLine($"\n\nDone.");
            Console.WriteLine($"Number of compares: {compares:N0}.");
            var remainingWords = validWords.Count > 20 ? "(Too many to list)" : string.Join(", ", validWords);
            Console.WriteLine($"Remaining words: {remainingWords}");
        }

        private IEnumerable<(string Key, int Score, int Count)> GetPatternsFromTwitter(int index)
        {
            static IEnumerable<(string Key, int Score, int Value)> ResultsSorter(Dictionary<string, (int freq, int score)> patterns, int frequencyThreshold)
            {
                return from p in patterns where p.Value.freq > frequencyThreshold orderby p.Value.score descending, p.Value descending select (p.Key, p.Value.score, p.Value.freq);
            }

            var userClient = new TwitterClient(
                "<CONSUMER_KEY>", // Consumer Key
                "<CONSUMER_SECRET>", // Consumer Secret
                "<ACCESS_TOKEN>" // Access Token
                // "ACCESS_TOKEN_SECRET" // Access Token Secret
            ); 

            var patterns = new Dictionary<string, (int frequency, int score)>();
            var tweetCount = 0;

            var res = userClient.Search.GetSearchTweetsIterator($"Wordle {index}");
            try
            {
                int patternsFound = 0;
                while (!res.Completed)
                {
                    var page = res.NextPageAsync(); 
                    var tweets = new List<ITweet>();
                    tweets.AddRange(page.Result);
                    Console.Write($"\rTweets processed: {tweetCount}. Still looking for {expectedPatterns-patternsFound} good patterns... ");

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
                                            var frequency = 0;
                                            if (patterns.ContainsKey(pattern))
                                            {
                                                frequency = patterns[pattern].frequency+1;
                                            } else {
                                                patterns[pattern] = (0, PatternScore(pattern));
                                            }

                                            patterns[pattern] = (frequency, patterns[pattern].score);

                                            if (patterns[pattern].frequency == frequencyThreshold && patterns[pattern].score >= scoreThreshold)
                                            {
                                                patternsFound++;
                                                if (patternsFound >= expectedPatterns)
                                                {
                                                  Console.WriteLine($"\rTweets processed: {tweetCount}. Done. Found total of {patterns.Count} patterns.");
                                                  return ResultsSorter(patterns, frequencyThreshold);
                                                }
                                            }
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
            return ResultsSorter(patterns, frequencyThreshold);
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
            if (compares % 1000 ==0)
            {
                Console.Write($"\rUsing pattern {pattern}. Testing {candidate}...");
            }

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
