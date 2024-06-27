using oldPhone.OldPhoneKeyboard;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows.Input;

namespace oldPhone
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string inputText;
        private string currentKey;
        private int currentIndex;
        private Timer keyPressTimer;
        private readonly Dictionary<string, string[]> keyMappings;
        private readonly HashSet<string> dictionary;

        public string InputText
        {
            get { return inputText; }
            set
            {
                inputText = value;
                OnPropertyChanged();
            }
        }

        public ICommand ButtonCommand { get; set; }

        public MainViewModel()
        {
            keyMappings = new Dictionary<string, string[]>
            {
                {"1", new[] {"1"}},
                {"2", new[] {"A", "B", "C"}},
                {"3", new[] {"D", "E", "F"}},
                {"4", new[] {"G", "H", "I"}},
                {"5", new[] {"J", "K", "L"}},
                {"6", new[] {"M", "N", "O"}},
                {"7", new[] {"P", "Q", "R", "S"}},
                {"8", new[] {"T", "U", "V"}},
                {"9", new[] {"W", "X", "Y", "Z"}},
                {"*", new[] {"*"}},
                {"0", new[] {"0"}},
                {"#", new[] {"#"}}
            };

            ButtonCommand = new RelayCommand(ExecuteButtonCommand);
            keyPressTimer = new Timer(1000);
            keyPressTimer.Elapsed += KeyPressTimer_Elapsed;
            string libPath = Path.Combine(Environment.CurrentDirectory, "dictionary.txt");
            dictionary = LoadDictionary(libPath);
            InputText = string.Empty;
        }

        private void ExecuteButtonCommand(object parameter)
        {
            if (parameter is string key)
            {
                if (key == "#")
                {
                    FormatLastWord();
                    return;
                }
                if (key == "*")
                {
                    if (InputText.Length > 0)
                    {
                        InputText = InputText.Substring(0, InputText.Length - 1);
                    }
                    return;
                }
                if (key == "0")
                {
                    if (InputText.Length > 0)
                        InputText += " ";
                    return;
                }

                if (currentKey != key)
                {
                    AddCurrentCharacterToInput();
                    currentKey = key;
                    currentIndex = 0;
                }
                else
                {
                    currentIndex = (currentIndex + 1) % keyMappings[key].Length;
                }

                keyPressTimer.Stop();
                keyPressTimer.Start();
            }
        }

        private void KeyPressTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            keyPressTimer.Stop();
            AddCurrentCharacterToInput();
        }

        private void AddCurrentCharacterToInput()
        {
            if (currentKey != null)
            {
                InputText += keyMappings[currentKey][currentIndex];
                currentKey = null;
            }
        }

        private void FormatLastWord()
        {
            if (!string.IsNullOrEmpty(InputText))
            {
                var words = InputText.Split(' ');
                var lastWord = words.Last();

                for (var i = 0; i < words.Length; i++)
                {
                    words[i] = GetCorrectedWord(words[i]);
                }
                InputText = string.Join(" ", words).ToUpper();
            }
        }

        private string GetCorrectedWord(string word)
        {
            if (dictionary.Contains(word.ToLower()))
            {
                return word;
            }

            string closestWord = null;
            int minDistance = int.MaxValue;

            var filteredDictionary = dictionary
                                    .Where(dictWord => Math.Abs(dictWord.Length - word.Length) <= 1)
                                    .ToList();

            foreach (var dictWord in filteredDictionary)
            {
                int distance = LevenshteinDistanceWithMapping(word.ToLower(), dictWord);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestWord = dictWord;
                }
            }

            return char.ToUpper(closestWord[0]) + closestWord.Substring(1);
        }

        private int LevenshteinDistanceWithMapping(string input, string dictWord)
        {
            if (string.IsNullOrEmpty(input)) return string.IsNullOrEmpty(dictWord) ? 0 : dictWord.Length;
            if (string.IsNullOrEmpty(dictWord)) return input.Length;

            int[,] costs = new int[input.Length + 1, dictWord.Length + 1];

            for (int i = 0; i <= input.Length; i++)
                costs[i, 0] = i * 2; 

            for (int j = 0; j <= dictWord.Length; j++)
                costs[0, j] = j * 2; 

            for (int i = 1; i <= input.Length; i++)
            {
                for (int j = 1; j <= dictWord.Length; j++)
                {
                    int cost = GetCharMappingCost(input[i - 1], dictWord[j - 1]);
                    costs[i, j] = Math.Min(
                        Math.Min(costs[i - 1, j] + 2, 
                                 costs[i, j - 1] + 2), 
                        costs[i - 1, j - 1] + cost);
                }
            }

            return costs[input.Length, dictWord.Length];
        }

        private int GetCharMappingCost(char inputChar, char dictChar)
        {
            if (char.ToUpper(inputChar) == char.ToUpper(dictChar))
            {
                return 0;
            }

            foreach (var keyMapping in keyMappings)
            {
                if (keyMapping.Value.Contains(inputChar.ToString().ToUpper()) &&
                    keyMapping.Value.Contains(dictChar.ToString().ToUpper()))
                {
                    return 1; 
                }
            }

            return 3;
        }

        private HashSet<string> LoadDictionary(string filePath)
        {
            if (File.Exists(filePath))
            {
                return new HashSet<string>(File.ReadAllLines(filePath).Select(w => w.Trim().ToLower()));
            }
            return new HashSet<string>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
