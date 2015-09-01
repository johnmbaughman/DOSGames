using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Runtime.InteropServices;
 
namespace Nibbles.Bas
{
    public struct SnakeBody
    {
        public int row, col;
    }
 
    /// <summary>This type defines the player's snake</summary>
    public struct SnakeType
    {
        public int head;
        public int length;
        public int row;
        public int col;
        public int direction;
        public int lives;
        public int score;
        public int scolor;
        public bool alive;
    }
 
    /// <summary>
    /// This type is used to represent the playing screen in memory
    /// It is used to simulate graphics in text mode, and has some interesting,
    /// and slightly advanced methods to increasing the speed of operation.
    /// Instead of the normal 80x25 text graphics using chr$(219) "¦", we will be
    /// using chr$(220)"_" and chr$(223) "¯" and chr$(219) "¦" to mimic an 80x50
    /// pixel screen.
    /// Check out sub-programs SET and POINTISTHERE to see how this is implemented
    /// feel free to copy these (as well as arenaType and the DIM ARENA stmt and the
    /// initialization code in the DrawScreen subprogram) and use them in your own
    /// programs
    /// </summary>
    public struct ArenaType
    {
        /// <summary>Maps the 80x50 point into the real 80x25</summary>
        public int realRow;
        /// <summary>Stores the current color of the point</summary>
        public int acolor;
        /// <summary>Each char has 2 points in it.  .SISTER is -1 if sister point is above, +1 if below</summary>
        public int sister;
    }
 
    public static class Program
    {
        // Constants
        public const int MAXSNAKELENGTH = 1000;
        public const int STARTOVER = 1;     // Parameters to 'Level' SUB
        public const int SAMELEVEL = 2;
        public const int NEXTLEVEL = 3;
 
        // Global variables
        public static ArenaType[,] arena = new ArenaType[51, 81]; // (1 TO 50, 1 TO 80)
        public static int curLevel;
        public static int[] colorTable = new int[10]; // (10)
 
        public static Random Rnd;
 
        static int originalWindowWidth, originalWindowHeight;
 
        public static void Main(string[] args)
        {
            originalWindowWidth = Console.WindowWidth;
            originalWindowHeight = Console.WindowHeight;
 
            try
            {
                bool isTrueTypeFont = ConsoleHelper.IsOutputConsoleFontTrueType();
                InitStrings(isTrueTypeFont);
 
                Console.Title = String.Format("Nibbles BAS [{0}]", isTrueTypeFont ? "TRUE TYPE" : "RASTER");
                Console.OutputEncoding = isTrueTypeFont ? Encoding.UTF8 : Encoding.Default;
                Console.SetWindowSize(80, 25);
                Console.CursorVisible = false;
                Console.CancelKeyPress += (s, e) => RestoreConsole();
 
                Rnd = new Random();
                // ClearKeyLocks();
                Intro();
                int NumPlayers, speed;
                string diff, monitor;
                GetInputs(out NumPlayers, out speed, out diff, out monitor);
                SetColors(monitor);
                DrawScreen();
 
                do
                    PlayNibbles(NumPlayers, speed, diff);
                while (StillWantsToPlay());
 
                RestoreConsole();
            }
            catch (Exception ex)
            {
                RestoreConsole();
                Console.WriteLine(ex);
            }
        }
 
        private static void RestoreConsole()
        {
            Console.ResetColor();
            Console.CursorVisible = true;
            Console.SetWindowSize(originalWindowWidth, originalWindowHeight);
            Console.Clear();
        }
 
        static char BlockChar;
        static char BlockUpperChar;
        static char BlockLowerChar;
        static string SpacePauseUpperLine;
        static string SpacePauseLowerLine;
        static string WelcomeHelpLine1, WelcomeHelpLine2, WelcomeHelpLine3;
        static string GameOverLine1, GameOverLine2, GameOverLine3, GameOverLine4, GameOverLine5;
 
        private static void InitStrings(bool isTrueTypeFont)
        {
            if (isTrueTypeFont)
            {
                BlockChar = '¦';
                BlockUpperChar = '¯';
                BlockLowerChar = '_';
                SpacePauseUpperLine = "¦¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¦";
                SpacePauseLowerLine = "¦_______________________________¦";
                WelcomeHelpLine1 = "P - Pause                ?                      W       ";
                WelcomeHelpLine2 = "                     (Left) ?   ? (Right)   (Left) A   D (Right)  ";
                WelcomeHelpLine3 = "                         ?                      S       ";
                GameOverLine1 = "¦¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¦";
                GameOverLine2 = "¦       G A M E   O V E R       ¦";
                GameOverLine3 = "¦                               ¦";
                GameOverLine4 = "¦      Play Again?   (Y/N)      ¦";
                GameOverLine5 = "¦_______________________________¦";
            }
            else
            {
                BlockChar = (char)219;
                BlockUpperChar = (char)223;
                BlockLowerChar = (char)220;
                SpacePauseUpperLine = new String(BlockChar, 1) + new String(BlockUpperChar, 31) + new String(BlockChar, 1);
                SpacePauseLowerLine = new String(BlockChar, 1) + new String(BlockLowerChar, 31) + new String(BlockChar, 1);
                // There isn't a good set of symbols for arrows in 8-bit ASCII so I just left them out
                WelcomeHelpLine1 = "P - Pause                                       W       ";
                WelcomeHelpLine2 = "                     (Left)       (Right)   (Left) A   D (Right)  ";
                WelcomeHelpLine3 = "                                                S       ";
                GameOverLine1 = SpacePauseUpperLine;
                GameOverLine2 = new String(BlockChar, 1) + "       G A M E   O V E R       " + new String(BlockChar, 1);
                GameOverLine3 = new String(BlockChar, 1) + new String(' ', 31) + new String(BlockChar, 1);
                GameOverLine4 = new String(BlockChar, 1) + "      Play Again?   (Y/N)      " + new String(BlockChar, 1);
                GameOverLine5 = SpacePauseLowerLine;
            }
        }
 
        private static void SetColors(string monitor)
        {
            if (monitor == "M")
                colorTable = new[] { 15, 7, 7, 0, 15, 0 };
            else
                colorTable = new[] { 14, 13, 12, 1, 15, 4 };
        }
 
        /// <summary>Centers text on given row</summary>
        private static void Center(int row, string text)
        {
            ConsoleSetCursorPosition(41 - text.Length / 2, row);
            Console.Write(text);
        }
 
        /// <summary>Draws playing field</summary>
        private static void DrawScreen()
        {
            // initialize screen
            // VIEW PRINT (?)
            Console.ForegroundColor = (ConsoleColor)colorTable[0];
            Console.BackgroundColor = (ConsoleColor)colorTable[3];
            Console.Clear();
 
            // Print title & message
            Center(1, "Nibbles!");
            Center(11, "Initializing Playing Field...");
 
            // Initialize arena array
            for (int row = 1; row <= 50; row++)
                for (int col = 1; col <= 80; col++)
                    arena[row, col] = new ArenaType { realRow = (row + 1) / 2, sister = (row % 2) * 2 - 1 };
        }
 
        /// <summary>Erases snake to facilitate moving through playing field</summary>
        private static void EraseSnake(SnakeType[] snake, SnakeBody[,] snakeBod, int snakeNum)
        {
            for (int c = 0; c < 9; c++)
                for (int b = snake[snakeNum].length - c; b >= 0; b -= 10)
                {
                    var tail = (snake[snakeNum].head + MAXSNAKELENGTH - b) % MAXSNAKELENGTH;
                    Set(snakeBod[tail, snakeNum].row, snakeBod[tail, snakeNum].col, colorTable[3]);
                    Thread.Sleep(2);
                }
        }
 
        /// <summary>Gets player inputs</summary>
        private static void GetInputs(out int NumPlayers, out int speed, out string diff, out string monitor)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();
            Console.CursorVisible = true;
 
            bool success;
            do
            {
                ConsoleSetCursorPosition(47, 5);
                Console.Write(new string(' ', 34));
                ConsoleSetCursorPosition(20, 5);
                Console.Write("How many players (1 or 2)");
                string num = Console.ReadLine();
                success = int.TryParse(num, out NumPlayers);
            }
            while (!success || NumPlayers < 1 || NumPlayers > 2);
 
            ConsoleSetCursorPosition(21, 8);
            Console.Write("Skill level (1 to 100)");
            ConsoleSetCursorPosition(22, 9);
            Console.Write("1   = Novice");
            ConsoleSetCursorPosition(22, 10);
            Console.Write("90  = Expert");
            ConsoleSetCursorPosition(22, 11);
            Console.Write("100 = Twiddle Fingers");
            ConsoleSetCursorPosition(15, 12);
            Console.Write("(Computer speed may affect your skill level)");
 
            do
            {
                ConsoleSetCursorPosition(44, 8);
                Console.Write(new string(' ', 35));
                ConsoleSetCursorPosition(43, 8);
                string gamespeed = Console.ReadLine();
                success = int.TryParse(gamespeed, out speed);
            }
            while (!success || speed < 1 || speed > 100);
 
            speed = (100 - speed) * 2 + 1;
 
            do
            {
                ConsoleSetCursorPosition(56, 15);
                Console.Write(new string(' ', 25));
                ConsoleSetCursorPosition(15, 15);
                Console.Write("Increase game speed during play (Y or N)");
                diff = Console.ReadLine().ToUpperInvariant();
            }
            while (diff != "Y" && diff != "N");
 
            do
            {
                ConsoleSetCursorPosition(46, 17);
                Console.Write(new string(' ', 34));
                ConsoleSetCursorPosition(17, 17);
                Console.Write("Monochrome or color monitor (M or C)");
                monitor = Console.ReadLine().ToUpperInvariant();
            }
            while (monitor != "M" && monitor != "C");
 
            Console.CursorVisible = false;
        }
 
        /// <summary>Initializes playing field colors</summary>
        private static void InitColors()
        {
            for (int row = 1; row <= 50; row++)
                for (int col = 1; col <= 80; col++)
                    arena[row, col].acolor = colorTable[3];
 
            Console.Clear();
 
            // Set (turn on) pixels for screen border
            for (int col = 1; col <= 80; col++)
            {
                Set(3, col, colorTable[2]);
                Set(50, col, colorTable[2]);
            }
            for (int row = 4; row <= 49; row++)
            {
                Set(row, 1, colorTable[2]);
                Set(row, 80, colorTable[2]);
            }
        }
 
        /// <summary>Displays game introduction</summary>
        private static void Intro()
        {
            Console.ForegroundColor = (ConsoleColor)15;
            Console.BackgroundColor = (ConsoleColor)0;
            Console.Clear();
 
            Center(4, "Q B a s i c   N i b b l e s");
            Console.ForegroundColor = (ConsoleColor)7;
            Center(6, "Copyright (C) Microsoft Corporation 1990");
            Center(8, "Nibbles is a game for one or two players.  Navigate your snakes");
            Center(9, "around the game board trying to eat up numbers while avoiding");
            Center(10, "running into walls or other snakes.  The more numbers you eat up,");
            Center(11, "the more points you gain and the longer your snake becomes.");
            Center(13, " Game Controls ");
            Center(15, "  General             Player 1               Player 2    ");
            Center(16, "                        (Up)                   (Up)      ");
            Center(17, WelcomeHelpLine1);
            Center(18, WelcomeHelpLine2);
            Center(19, WelcomeHelpLine3);
            Center(20, "                       (Down)                 (Down)     ");
            Center(24, "Press any key to continue");
 
            // PLAY "MBT160O1L8CDEDCDL4ECC"
            SparklePause();
        }
 
        /// <summary>Sets game level</summary>
        private static void Level(int WhatToDO, SnakeType[] sammy)
        {
            switch (WhatToDO)
            {
                case STARTOVER:
                    curLevel = 1;
                    break;
 
                case NEXTLEVEL:
                    curLevel = curLevel + 1;
                    break;
            }
 
            // Initialize Snakes
            sammy[0].head = 1;
            sammy[0].length = 2;
            sammy[0].alive = true;
            sammy[1].head = 1;
            sammy[1].length = 2;
            sammy[1].alive = true;
 
            InitColors();
 
            switch (curLevel)
            {
                case 1:
                    sammy[0].row = 25;
                    sammy[1].row = 25;
                    sammy[0].col = 50;
                    sammy[1].col = 30;
                    sammy[0].direction = 4;
                    sammy[1].direction = 3;
                    break;
 
                case 2:
                    for (int i = 20; i <= 60; i++)
                        Set(25, i, colorTable[2]);
                    sammy[0].row = 7;
                    sammy[1].row = 43;
                    sammy[0].col = 60;
                    sammy[1].col = 20;
                    sammy[0].direction = 3;
                    sammy[1].direction = 4;
                    break;
 
                case 3:
                    for (int i = 10; i <= 40; i++)
                    {
                        Set(i, 20, colorTable[2]);
                        Set(i, 60, colorTable[2]);
                    }
                    sammy[0].row = 25;
                    sammy[1].row = 25;
                    sammy[0].col = 50;
                    sammy[1].col = 30;
                    sammy[0].direction = 1;
                    sammy[1].direction = 2;
                    break;
 
                case 4:
                    for (int i = 4; i <= 30; i++)
                    {
                        Set(i, 20, colorTable[2]);
                        Set(53 - i, 60, colorTable[2]);
                    }
                    for (int i = 2; i <= 40; i++)
                    {
                        Set(38, i, colorTable[2]);
                        Set(15, 81 - i, colorTable[2]);
                    }
                    sammy[0].row = 7;
                    sammy[1].row = 43;
                    sammy[0].col = 60;
                    sammy[1].col = 20;
                    sammy[0].direction = 3;
                    sammy[1].direction = 4;
                    break;
 
                case 5:
                    for (int i = 13; i <= 39; i++)
                    {
                        Set(i, 21, colorTable[2]);
                        Set(i, 59, colorTable[2]);
                    }
                    for (int i = 23; i <= 57; i++)
                    {
                        Set(11, i, colorTable[2]);
                        Set(41, i, colorTable[2]);
                    }
                    sammy[0].row = 25;
                    sammy[1].row = 25;
                    sammy[0].col = 50;
                    sammy[1].col = 30;
                    sammy[0].direction = 1;
                    sammy[1].direction = 2;
                    break;
 
                case 6:
                    for (int i = 4; i <= 49; i++)
                    {
                        if (i > 30 || i < 23)
                        {
                            Set(i, 10, colorTable[2]);
                            Set(i, 20, colorTable[2]);
                            Set(i, 30, colorTable[2]);
                            Set(i, 40, colorTable[2]);
                            Set(i, 50, colorTable[2]);
                            Set(i, 60, colorTable[2]);
                            Set(i, 70, colorTable[2]);
                        }
                    }
                    sammy[0].row = 7;
                    sammy[1].row = 43;
                    sammy[0].col = 65;
                    sammy[1].col = 15;
                    sammy[0].direction = 2;
                    sammy[1].direction = 1;
                    break;
 
                case 7:
                    for (int i = 4; i <= 49; i += 2)
                        Set(i, 40, colorTable[2]);
                    sammy[0].row = 7;
                    sammy[1].row = 43;
                    sammy[0].col = 65;
                    sammy[1].col = 15;
                    sammy[0].direction = 2;
                    sammy[1].direction = 1;
                    break;
 
                case 8:
                    for (int i = 4; i <= 40; i++)
                    {
                        Set(i, 10, colorTable[2]);
                        Set(53 - i, 20, colorTable[2]);
                        Set(i, 30, colorTable[2]);
                        Set(53 - i, 40, colorTable[2]);
                        Set(i, 50, colorTable[2]);
                        Set(53 - i, 60, colorTable[2]);
                        Set(i, 70, colorTable[2]);
                    }
                    sammy[0].row = 7;
                    sammy[1].row = 43;
                    sammy[0].col = 65;
                    sammy[1].col = 15;
                    sammy[0].direction = 2;
                    sammy[1].direction = 1;
                    break;
 
                case 9:
                    for (int i = 6; i <= 47; i++)
                    {
                        Set(i, i, colorTable[2]);
                        Set(i, i + 28, colorTable[2]);
                    }
                    sammy[0].row = 40;
                    sammy[1].row = 15;
                    sammy[0].col = 75;
                    sammy[1].col = 5;
                    sammy[0].direction = 1;
                    sammy[1].direction = 2;
                    break;
 
                default:
                    for (int i = 4; i <= 49; i += 2)
                    {
                        Set(i, 10, colorTable[2]);
                        Set(i + 1, 20, colorTable[2]);
                        Set(i, 30, colorTable[2]);
                        Set(i + 1, 40, colorTable[2]);
                        Set(i, 50, colorTable[2]);
                        Set(i + 1, 60, colorTable[2]);
                        Set(i, 70, colorTable[2]);
                    }
                    sammy[0].row = 7;
                    sammy[1].row = 43;
                    sammy[0].col = 65;
                    sammy[1].col = 15;
                    sammy[0].direction = 2;
                    sammy[1].direction = 1;
                    break;
            }
        }
 
        /// <summary>Main routine that controls game play</summary>
        private static void PlayNibbles(int NumPlayers, int speed, string diff)
        {
            // Initialize Snakes
            SnakeBody[,] sammyBody = new SnakeBody[MAXSNAKELENGTH, 2];
            SnakeType[] sammy = new SnakeType[2];
            sammy[0] = new SnakeType { lives = 5, score = 0, scolor = colorTable[0] };
            sammy[1] = new SnakeType { lives = 5, score = 0, scolor = colorTable[1] };
 
            Level(STARTOVER, sammy);
            var startRow1 = sammy[0].row;
            var startCol1 = sammy[0].col;
            var startRow2 = sammy[1].row;
            var startCol2 = sammy[1].col;
 
            var curSpeed = speed;
 
            // play Nibbles until finished
 
            SpacePause("     Level " + curLevel + ",  Push Space");
 
            do
            {
                if (NumPlayers == 1)
                    sammy[1].row = 0;
 
                var number = 1; // Current number that snakes are trying to run into
                var nonum = true; // nonum = TRUE if a number is not on the screen
                int numberRow = 0, NumberCol = 0, sisterRow = 0;
 
                var playerDied = false;
                PrintScore(NumPlayers, sammy[0].score, sammy[1].score, sammy[0].lives, sammy[1].lives);
                // PLAY "T160O1>L20CDEDCDL10ECC"
 
                do
                {
                    // Print number if no number exists
                    if (nonum)
                    {
                        do
                        {
                            numberRow = (int)(Rnd.NextDouble() * 47 + 3);
                            NumberCol = (int)(Rnd.NextDouble() * 78 + 2);
                            sisterRow = numberRow + arena[numberRow, NumberCol].sister;
                        }
                        while (PointIsThere(numberRow, NumberCol, colorTable[3]) || PointIsThere(sisterRow, NumberCol, colorTable[3]));
                        numberRow = arena[numberRow, NumberCol].realRow;
                        nonum = false;
                        Console.ForegroundColor = (ConsoleColor)colorTable[0];
                        Console.BackgroundColor = (ConsoleColor)colorTable[3];
                        ConsoleSetCursorPosition(NumberCol, numberRow);
                        Console.Write(number.ToString().Last());
                    }
 
                    // Delay game
                    Thread.Sleep(curSpeed);
 
                    // Get keyboard input & Change direction accordingly
                    if (Console.KeyAvailable)
                    {
                        var kbd = Console.ReadKey(true).Key;
                        switch (kbd)
                        {
                            case ConsoleKey.W: if (sammy[1].direction != 2) sammy[1].direction = 1; break;
                            case ConsoleKey.S: if (sammy[1].direction != 1) sammy[1].direction = 2; break;
                            case ConsoleKey.A: if (sammy[1].direction != 4) sammy[1].direction = 3; break;
                            case ConsoleKey.D: if (sammy[1].direction != 3) sammy[1].direction = 4; break;
                            case ConsoleKey.UpArrow: if (sammy[0].direction != 2) sammy[0].direction = 1; break;
                            case ConsoleKey.DownArrow: if (sammy[0].direction != 1) sammy[0].direction = 2; break;
                            case ConsoleKey.LeftArrow: if (sammy[0].direction != 4) sammy[0].direction = 3; break;
                            case ConsoleKey.RightArrow: if (sammy[0].direction != 3) sammy[0].direction = 4; break;
                            case ConsoleKey.Spacebar: case ConsoleKey.P: SpacePause(" Game Paused ... Push Space  "); break;
                            default: break;
                        }
                    }
 
                    for (int a = 0; a < NumPlayers; a++)
                    {
                        // Move Snake
                        switch (sammy[a].direction)
                        {
                            case 1: sammy[a].row = sammy[a].row - 1; break;
                            case 2: sammy[a].row = sammy[a].row + 1; break;
                            case 3: sammy[a].col = sammy[a].col - 1; break;
                            case 4: sammy[a].col = sammy[a].col + 1; break;
                        }
 
                        // If snake hits number, respond accordingly
                        if (numberRow == (sammy[a].row + 1) / 2 && NumberCol == sammy[a].col)
                        {
                            // PLAY "MBO0L16>CCCE"
                            if (sammy[a].length < MAXSNAKELENGTH - 30)
                                sammy[a].length = sammy[a].length + number * 4;
 
                            sammy[a].score = sammy[a].score + number;
                            PrintScore(NumPlayers, sammy[0].score, sammy[1].score, sammy[0].lives, sammy[1].lives);
                            number = number + 1;
                            if (number == 10)
                            {
                                EraseSnake(sammy, sammyBody, 0);
                                EraseSnake(sammy, sammyBody, 1);
                                ConsoleSetCursorPosition(NumberCol, numberRow);
                                Console.Write(" ");
                                Level(NEXTLEVEL, sammy);
                                PrintScore(NumPlayers, sammy[0].score, sammy[1].score, sammy[0].lives, sammy[1].lives);
                                SpacePause("     Level " + curLevel + ",  Push Space");
                                if (NumPlayers == 1)
                                    sammy[1].row = 0;
                                number = 1;
                                if (diff == "P")
                                {
                                    speed = speed - 10;
                                    curSpeed = speed;
                                }
                            }
                            nonum = true;
                            if (curSpeed < 1)
                                curSpeed = 1;
                        }
                    }
 
                    for (int a = 0; a < NumPlayers; a++)
                    {
                        // If player runs into any point, or the head of the other snake, it dies.
                        if (PointIsThere(sammy[a].row, sammy[a].col, colorTable[3]) || (sammy[0].row == sammy[1].row && sammy[0].col == sammy[1].col))
                        {
                            // PLAY "MBO0L32EFGEFDC"
                            Console.BackgroundColor = (ConsoleColor)colorTable[3];
                            ConsoleSetCursorPosition(NumberCol, numberRow);
                            Console.Write(" ");
                            playerDied = true;
                            sammy[a].alive = false;
                            sammy[a].lives = sammy[a].lives - 1;
                        }
                        // Otherwise, move the snake, and erase the tail
                        else
                        {
                            sammy[a].head = (sammy[a].head + 1) % MAXSNAKELENGTH;
                            sammyBody[sammy[a].head, a].row = sammy[a].row;
                            sammyBody[sammy[a].head, a].col = sammy[a].col;
                            var tail = (sammy[a].head + MAXSNAKELENGTH - sammy[a].length) % MAXSNAKELENGTH;
                            Set(sammyBody[tail, a].row, sammyBody[tail, a].col, colorTable[3]);
                            sammyBody[tail, a].row = 0;
                            Set(sammy[a].row, sammy[a].col, sammy[a].scolor);
                        }
                    }
                }
                while (!playerDied);
 
                // reset speed to initial value
                curSpeed = speed;
 
                for (int a = 0; a < NumPlayers; a++)
                {
                    EraseSnake(sammy, sammyBody, a);
 
                    // If dead, then erase snake in really cool way
                    if (!sammy[a].alive)
                    {
                        // Update score
                        sammy[a].score = sammy[a].score - 10;
                        PrintScore(NumPlayers, sammy[0].score, sammy[1].score, sammy[0].lives, sammy[1].lives);
 
                        if (a == 0)
                            SpacePause(" Sammy Dies! Push Space! --->");
                        else
                            SpacePause(" <---- Jake Dies! Push Space ");
                    }
                }
 
                Level(SAMELEVEL, sammy);
                PrintScore(NumPlayers, sammy[0].score, sammy[1].score, sammy[0].lives, sammy[1].lives);
 
                // Play next round, until either of snake's lives have run out.
            }
            while (sammy[0].lives != 0 && sammy[1].lives != 0);
        }
 
        /// <summary>Checks the global  arena array to see if the boolean flag is set</summary>
        private static bool PointIsThere(int row, int col, int acolor)
        {
            if (row != 0)
                return (arena[row, col].acolor != acolor);
            return false;
        }
 
        /// <summary>Prints players scores and number of lives remaining</summary>
        private static void PrintScore(int NumPlayers, int score1, int score2, int lives1, int lives2)
        {
            Console.ForegroundColor = (ConsoleColor)15;
            Console.BackgroundColor = (ConsoleColor)colorTable[3];
 
            if (NumPlayers == 2)
            {
                ConsoleSetCursorPosition(1, 1);
                Console.Write(string.Format("{0:0,0}  Lives: {1}  <--JAKE", score2, lives2));
            }
            ConsoleSetCursorPosition(49, 1);
            Console.Write(string.Format("SAMMY-->  Lives: {0}     {1:0,0}", lives1, score1));
        }
 
        /// <summary>Sets row and column on playing field to given color to facilitate moving of snakes around the field.</summary>
        private static void Set(int row, int col, int acolor)
        {
            if ((row == 50 || row == 49) && col == 80)
                return;
            if (row != 0)
            {
                // assign color to arena
                arena[row, col].acolor = acolor;
                // Get real row of pixel
                var realRow = arena[row, col].realRow;
                // Deduce whether pixel is on top¯, or bottom_
                bool topFlag = arena[row, col].sister == 1;
                // Get arena row of sister
                var sisterRow = row + arena[row, col].sister;
                // Determine sister's color
                var sisterColor = arena[sisterRow, col].acolor;
 
                ConsoleSetCursorPosition(col, realRow);
 
                // If both points are same
                if (acolor == sisterColor)
                {
                    Console.ForegroundColor = (ConsoleColor)acolor;
                    Console.BackgroundColor = (ConsoleColor)acolor;
                    Console.Write(BlockChar);
                }
                else
                {
                    if (topFlag)
                    {
                        Console.ForegroundColor = (ConsoleColor)acolor;
                        Console.BackgroundColor = (ConsoleColor)sisterColor;
                        Console.Write(BlockUpperChar);
                    }
                    else
                    {
                        Console.ForegroundColor = (ConsoleColor)acolor;
                        Console.BackgroundColor = (ConsoleColor)sisterColor;
                        Console.Write(BlockLowerChar);
                    }
                }
            }
        }
 
        /// <summary>Pauses game play and waits for space bar to be pressed before continuing</summary>
        private static void SpacePause(string text)
        {
            Console.ForegroundColor = (ConsoleColor)colorTable[4];
            Console.BackgroundColor = (ConsoleColor)colorTable[5];
            Center(11, SpacePauseUpperLine);
            Center(12, new String(BlockChar, 1) + (text + new String(' ', 31)).Substring(0, 31) + new String(BlockChar, 1));
            Center(13, SpacePauseLowerLine);
 
            while (Console.KeyAvailable)
                Console.ReadKey(true);
            while (Console.ReadKey(true).Key != ConsoleKey.Spacebar) { }
            Console.ForegroundColor = (ConsoleColor)15;
            Console.BackgroundColor = (ConsoleColor)colorTable[3];
 
            // Restore the screen background
            for (int i = 21; i <= 26; i++)
            {
                for (int j = 24; j <= 57; j++)
                {
                    Set(i, j, arena[i, j].acolor);
                }
            }
        }
 
        /// <summary>Creates flashing border for intro screen</summary>
        private static void SparklePause()
        {
            Console.ForegroundColor = (ConsoleColor)4;
            Console.BackgroundColor = (ConsoleColor)0;
 
            bool stop = false;
            var t = new Thread(() =>
            {
                string aa = "*    *    *    *    *    *    *    *    *    *    *    *    *    *    *    *    *    ";
 
                while (!stop)
                {
                    for (int a = 1; a <= 5; a++)
                    {
                        Thread.Sleep(50);
 
                        // print horizontal sparkles
                        ConsoleSetCursorPosition(1, 1);
                        Console.Write(aa.Substring(a, 80));
                        ConsoleSetCursorPosition(1, 22);
                        Console.Write(aa.Substring(6 - a, 80));
 
                        // Print Vertical sparkles
                        for (int b = 2; b <= 21; b++)
                        {
                            var c = (a + b) % 5;
                            if (c == 1)
                            {
                                ConsoleSetCursorPosition(80, b);
                                Console.Write("*");
                                ConsoleSetCursorPosition(1, 23 - b);
                                Console.Write("*");
                            }
                            else
                            {
                                ConsoleSetCursorPosition(80, b);
                                Console.Write(" ");
                                ConsoleSetCursorPosition(1, 23 - b);
                                Console.Write(" ");
                            }
                        }
                    }
                }
            });
            t.Start();
            Console.ReadKey(true);
            stop = true;
            t.Join();
        }
 
        /// <summary>Determines if users want to play game again.</summary>
        private static bool StillWantsToPlay()
        {
            Console.ForegroundColor = (ConsoleColor)colorTable[4];
            Console.BackgroundColor = (ConsoleColor)colorTable[5];
            Center(10, GameOverLine1);
            Center(11, GameOverLine2);
            Center(12, GameOverLine3);
            Center(13, GameOverLine4);
            Center(14, GameOverLine5);
 
            char kbd;
            do
                kbd = char.ToUpperInvariant(Console.ReadKey(true).KeyChar);
            while (kbd != 'Y' && kbd != 'N');
 
            Console.ForegroundColor = (ConsoleColor)15;
            Console.BackgroundColor = (ConsoleColor)colorTable[3];
            Center(10, "                                 ");
            Center(11, "                                 ");
            Center(12, "                                 ");
            Center(13, "                                 ");
            Center(14, "                                 ");
 
            if (kbd == 'N')
            {
                Console.ForegroundColor = (ConsoleColor)7;
                Console.BackgroundColor = (ConsoleColor)0;
                Console.Clear();
            }
 
            return (kbd == 'Y');
        }
 
        private static void ConsoleSetCursorPosition(int x, int y)
        {
            Console.SetCursorPosition(x - 1, y - 1);
        }
    }
 
    static class ConsoleHelper
    {
        const int TMPF_TRUETYPE = 0x4;
        const int LF_FACESIZE = 32;
        const int STD_INPUT_HANDLE = -10;
        const int STD_OUTPUT_HANDLE = -11;
        const int STD_ERROR_HANDLE = -12;
 
        readonly static IntPtr InvalidHandleValue = new IntPtr(-1);
 
        [DllImport("Kernel32.dll", SetLastError = true)]
        extern static IntPtr GetStdHandle(int handle);
 
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        extern static bool GetCurrentConsoleFontEx(IntPtr hConsoleOutput, bool bMaximumWindow, [In, Out] ref CONSOLE_FONT_INFOEX lpConsoleCurrentFont);
 
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct CONSOLE_FONT_INFOEX
        {
            public int cbSize;
            public int Index;
            public short Width;
            public short Height;
            public int Family;
            public int Weight;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LF_FACESIZE)]
            public string FaceName;
        }
 
        public static IntPtr GetOutputHandle()
        {
            IntPtr handle = GetStdHandle(STD_OUTPUT_HANDLE);
            if (handle == InvalidHandleValue)
                throw new Win32Exception();
            return handle;
        }
 
        public static bool IsOutputConsoleFontTrueType()
        {
            CONSOLE_FONT_INFOEX cfi = new CONSOLE_FONT_INFOEX();
            cfi.cbSize = Marshal.SizeOf(typeof(CONSOLE_FONT_INFOEX));
            if (GetCurrentConsoleFontEx(GetOutputHandle(), false, ref cfi))
                return (cfi.Family & TMPF_TRUETYPE) == TMPF_TRUETYPE;
            return false;
        }
    }
}