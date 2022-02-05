# WordleSolver

```
Wordle 212 6/6

⬜⬜⬜⬜🟩
⬜🟨🟩⬜🟩
🟨⬜🟩🟨🟩
🟩⬜🟩🟩🟩
🟩⬜🟩🟩🟩
🟩🟩🟩🟩🟩
```

The purpose of this project was to explore the possibility of guessing the [Wordle](https://en.wikipedia.org/wiki/Wordle) word using posts people share of their results in the game (see example above). 
## Terminology
- Result word - the correct daily Wordle word
- Guessed word - a word that the user guesses (may or may not be the same as the Result word)
- Hint/Pattern - a list of 5 colored squares indicating how each letter in the Guessed word matches the Result word
  - ⬜ - No match. The letter in the Guessed word doesn't appear in the Result word
  - 🟨 - Partial match - The letter in the Guessed word appear in the Result word but in a different index
  - 🟩 - Exact match - The letter in the Guessed word matches the letter in the Result word in the same index

## The Approach
The rational is that using enough posts, and given a known finite list of words that the game supports, it will be possible to rule out words by seeing if a word exists that could match the shown pattern.

For example, the pattern 🟩⬜🟩🟩🟩 suggests that there is a word that is only different from the result word by the second letter. This means that using this pattern we can rule out that the Result word is `panic` because there is no other word in the dictionary that starts with `p` and ends with `nic`. 

In addition, this pattern also tells us that the second letter in the Guess word doesn't exist in the Result word. This means that we can rule out the word `nival` from being the Result word, because the only word in the dictionary that matches it with a different 2nd letter is `naval`, but if `naval` was the Guess word, then the resulting pattern should have been 🟩🟨🟩🟩🟩 (which is not even a valid pattern).



Using this information, it is possible to go over all the words in the dictionary, and check for each if such a word exists. If not, then this means that word cannot be the result given this hint pattern. The idea is that with enough patterns, it will be possible to narrow the dictionary to 1 word. 

The runtime complexity of this approach is `n^2`, while the size of the dictionary the game uses is `12,972` which means this is not too bad and takes a few seconds on a regular CPU. 
During this `n^2` double-loop we also iterate through all the found patterns, which there are about `40` of. At the end of the full cycle of a single pattern, there are some words that gets ruled out. They will be excluded from the next run, so in practice the execution is less than `n^2 x p` where `p` is the number of patterns. 

## Pattern (Hint) Harvesting

This approach is very susceptible to bad patterns. This seems to happen a lot. Fake posts on Twitter, as well as posts from Wordle in other languages will throw-off the algorithm and will make it miss. 
For this reason, it is important to correctly pick the right patterns. In addition, testing the strongest patterns first will reduce the overall runtime of the algorithm. 

I used the following methods to make sure the algorithm picks the best patterns:

1. A pattern must be "seen" in Twitter posts at least `5` times before being used. This is controlled by the `frequencyThreshold` constant.
2. A pattern must be above a certain score as determined by the `scoreThreshold` constant.

### Pattern Score 
Pattern Score is determined using the following method: for each letter in the pattern, we assign a score based on whether it is -, + or ?. The pattern score is a sum of all 5 values. 
```
- = -2 points
+ = +3 points
? = +2 points
```
This means that a pattern score can range between `-6` for e.g. ⬜⬜⬜⬜🟨, which in practice is a useless hint since it does eliminate *any* word in the dictionary, to `14` for e.g. 🟩🟩🟨🟩🟩, which eliminates *90%* of the words in the dictionary. 

## Credits
- This whole thing started from a [Tweet](https://twitter.com/ianmercer/status/1482381219585150977) by [Ian Mercer](https://twitter.com/ianmercer) who suggested that this thing is possible.
>Meta Wordle : create an algorithm that uses all the colored squares posted to social media to solve it on the first try.
- An earlier, more efficient version of the same is also available from [David Ebbo](https://twitter.com/davidebbo) in [Github|WordleReverseSolver](https://github.com/davidebbo/WordleReverseSolver). Also thanks David for the bug fixes.
