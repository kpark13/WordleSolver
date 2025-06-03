using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WordleSolver;
//Our strategy uses a conservative approach for the first three guesses followed by an elimination process based on the information gained from the previous guesses. On the first guess, it always picks the word "spine", because of its common letters to maximize information gain. If that guess doesn't solve the puzzle, it then guesses 'tardy" on the second turn, because it is another word with common letters to further gain information. If a third guess is needed, it uses "jumbo", because of its distinct vowels. After every guess the algorithm filters the remaining possible words by comparing the guess results (correct position, wrong position, or unused) to remove any impossible words. From the fourth guess on, or earlier if only three possible words remains, it simply picks the first remaining word in the filtered list. This strategy is simple but effective. it starts with guesses that provide high information, then narrows down the solution based on Wordle's letter results rules.

namespace WordleSolver.Strategies
{
    public sealed class MySolver : IWordleSolverStrategy
    {
        private static readonly string WordListPath = Path.Combine("data", "wordle.txt");

        private static readonly List<string> WordList = LoadWordList();

        private List<string> _remainingWords = new();

       
        private static List<string> LoadWordList()
        {
            if (!File.Exists(WordListPath))
                throw new FileNotFoundException($"Word list not found at path: {WordListPath}");

            return File.ReadAllLines(WordListPath)
                .Select(w => w.Trim().ToLowerInvariant()) 
                .Where(w => w.Length == 5) 
                .Distinct() 
                .ToList();
        }

        public void Reset()
        {
            _remainingWords = new List<string>(WordList);
        }

        public string PickNextGuess(GuessResult previousResult)
        {
            if (!previousResult.IsValid)
                throw new InvalidOperationException("PickNextGuess shouldn't be called if previous result isn't valid");

            // If we're past the first guess, filter the word list based on feedback
            if (previousResult.GuessNumber > 0)
            {
                string lastGuess = previousResult.Word;
                var statuses = previousResult.LetterStatuses;

                // Filter out words that don't match feedback from the last guess
                _remainingWords = _remainingWords.Where(candidate =>
                {
                    for (int i = 0; i < 5; i++)
                    {
                        char guessedChar = lastGuess[i];
                        char candidateChar = candidate[i];

                        if (statuses[i] == LetterStatus.Correct)
                        {
                            // Letter must be in the correct position
                            if (candidateChar != guessedChar)
                                return false;
                        }
                        else if (statuses[i] == LetterStatus.Misplaced)
                        {
                            // Letter must be present but in a different position
                            if (candidateChar == guessedChar || !candidate.Contains(guessedChar))
                                return false;
                        }
                        else if (statuses[i] == LetterStatus.Unused)
                        {
                            // Check if letter is truly unused (not in any other position)
                            bool guessedElsewhere = false;
                            for (int j = 0; j < 5; j++)
                            {
                                if (j != i && lastGuess[j] == guessedChar && statuses[j] != LetterStatus.Unused)
                                {
                                    guessedElsewhere = true;
                                    break;
                                }
                            }

                            // If not used elsewhere, remove candidates that still have it
                            if (!guessedElsewhere && candidate.Contains(guessedChar))
                                return false;
                        }
                    }
                    return true; // Keep this word
                }).ToList();
            }

            // Hardcoded guesses for the first three rounds to gain information
            if (previousResult.Guesses.Count == 0)
                return "spine";

            if (previousResult.Guesses.Count == 1 && _remainingWords.Count < 4)
                return _remainingWords.First();

            if (previousResult.Guesses.Count == 1)
                return "tardy";

            if (previousResult.Guesses.Count == 2 && _remainingWords.Count < 4)
                return _remainingWords.First();

            if (previousResult.Guesses.Count == 2)
                return "jumbo";

            // From 4th guess onward, choose from remaining words
            string choice = ChooseBestRemainingWord(previousResult);
            _remainingWords.Remove(choice); // Avoid guessing it again
            return choice;
        }

        public string ChooseBestRemainingWord(GuessResult previousResult)
        {
            return _remainingWords.First();
        }
    }
}