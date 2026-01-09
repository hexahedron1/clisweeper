// See https://aka.ms/new-console-template for more information

using System.Drawing;
using static System.Console;
using GNUArgParser;
using Newtonsoft.Json;

string configPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "clisweeper");
if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);
string defaultFile = Path.Join(configPath, "default.json");
if (!File.Exists(defaultFile)) File.WriteAllLines(defaultFile, [ "{", "    \"width\": 16,", "    \"height\": 16,", "    \"mines\": 32", "}" ]);
string json = File.ReadAllText(defaultFile);
var anonymousetypeobjeje = JsonConvert.DeserializeAnonymousType(json, new { width = 0, height = 0, mines = 0 });
if (anonymousetypeobjeje == null) {
    File.WriteAllLines(defaultFile, ["{", "    \"width\": 16,", "    \"height\": 16,", "    \"mines\": 32", "}"]);
    anonymousetypeobjeje = new { width = 16, height = 16, mines = 32 };
}
int width = anonymousetypeobjeje.width;
int height = anonymousetypeobjeje.height;
int minec = anonymousetypeobjeje.mines;
var arg = ArgumentParser.Parse(args);
if (arg.Flags.Contains("help") || arg.Flags.Contains("h")) {
    WriteLine("\e[1mOptions");
    WriteLine("\e[1m(No options)     \e[0m starts a new game with default parameters");
    WriteLine("\e[1m-h --help        \e[0m displays this message");
    WriteLine("\e[1m-d --defaults    \e[0m displays the default game parameters");
    WriteLine("\e[1m--width=n        \e[0m sets the width of the field");
    WriteLine("\e[1m--height=n       \e[0m sets the height of the field");
    WriteLine("\e[1m--mines=n        \e[0m sets the mine count");
    WriteLine("\e[1m-w --window      \e[0m sets the field size to fill your whole terminal window (or screen in a tty); 1/8 of all cells will be mines");
    WriteLine("\e[1m-D --set-defaults\e[0m saves new default field size (used with options above)");
    WriteLine("\e[1m--timer=t        \e[0m sets a time constraint in seconds");
    WriteLine("\n\e[1mHow to play\e[0m");
    WriteLine("In case you don't know how to play minesweeper, here's a quick guide");
    WriteLine("The play field is a grid of cells");
    WriteLine("Some cells contain mines in them");
    WriteLine("Cells can be 'dug', revealing the number of mines around that cell (in a square)");
    WriteLine("Cells that can be dug are marked with dots");
    WriteLine("If you dig a cell that has a mine, you lose");
    WriteLine("You can mark an untouched cell with a flag (represented by \e[95m¶ \e[0m)");
    WriteLine("Your goal is to mark all mines with flags correctly");
    WriteLine("If you lose, flags will be highlighted based on whether they are correctly placed");
    WriteLine("\n\e[1mControls\e[0m");
    WriteLine("\e[1mWASD / Arrow keys\e[0m move cursor");
    WriteLine("\e[1mSpace            \e[0m toggle flag");
    WriteLine("\e[1mEnter            \e[0m dig");
    WriteLine("\e[1mQ                \e[0m exit");
    WriteLine("\n\e[1mGameplay tips\e[0m");
    WriteLine("Cells with no mines around them will automatically reveal adjacent cells (this will cascade)");
    WriteLine("If a revealed cell has the correct amount of flags around it, it can be dug to automatically reveal all untouched cells around it\n");
    return;
}
if (arg.Flags.Contains("defaults") || arg.Flags.Contains("d")) {
    WriteLine($"Width: \e[1m{anonymousetypeobjeje.width}\e[0m\nHeight: \e[1m{anonymousetypeobjeje.height}\e[0m\nMines: \e[1m{anonymousetypeobjeje.mines}\e[0m\n");
    return;
}
try {
    if (arg.Options.TryGetValue("width", out var sw)) width = int.Parse(sw);
    if (arg.Options.TryGetValue("height", out var sh)) height = int.Parse(sh);
    if (arg.Options.TryGetValue("mines", out var sm)) minec = int.Parse(sm);
} catch (FormatException) {
    WriteLine("One of the arguments is improperly formatted\n");
    return;
}

if (arg.Flags.Contains("window") || arg.Flags.Contains("w")) {
    width = WindowWidth/2 - 4;
    height = WindowHeight - 8;
    minec = width * height / 8;
}

if (arg.Flags.Contains("set-defaults") || arg.Flags.Contains("D")) {
    anonymousetypeobjeje = new {
        width,
        height,
        mines = minec
    };
    File.WriteAllText(defaultFile, JsonConvert.SerializeObject(anonymousetypeobjeje));
    WriteLine("New defaults saved\n");
    return;
}
int? timer = null;
try {
    if (arg.Options.TryGetValue("timer", out var st)) timer = int.Parse(st);
} catch (FormatException) {
    WriteLine("One of the arguments is improperly formatted\n");
    return;
}

int?[,] grid = new int?[width, height];
for (int x = 0; x < width; x++) {
    for (int y =  0; y < height; y++)
        grid[x, y] = null;
}

if (width < 8 || height < 8) {
    WriteLine("The minimum field size is 16x16\n");
    return;
}

int ww = width*2 + 2;
int hh = height + 5;
int ox = WindowWidth / 2 - ww / 2;
int oy = WindowHeight / 2 - hh / 2;
bool redraw = true;
int cx = 0;
int cy = 0;
bool[,] mines = new bool[width, height];
int remaining = minec;
Random rand = new Random();
while (remaining > 0) {
    int mx = rand.Next(width);
    int my = rand.Next(height);
    if ((mx != 0 || my != 0) && !mines[mx, my]) {
        mines[mx, my] = true;
        remaining--;
    }
}

DateTime gameStart = DateTime.Now;
int cellsRemaining = width * height;
bool lost = false;
List<(int, int)> flags = [];
void Dig(int x, int y) {
    if (flags.Contains((x, y))) return;
    if (grid[x, y] == null) {
        redraw = true;
        if (mines[x, y]) {
            grid[x, y] = -1;
            lost = true;
            return;
        }

        cellsRemaining--;
        int around = 0;
        for (int dx = -1; dx < 2; dx++) {
            for (int dy = -1; dy < 2; dy++) {
                if (x + dx < 0 || x + dx >= width || y + dy < 0 || y + dy >= height) continue;
                if (mines[x + dx, y + dy]) around++;
            }
        }
        grid[x, y] = around;
        if (around == 0) {
            for (int dx = -1; dx < 2; dx++) {
                for (int dy = -1; dy < 2; dy++) {
                    if (x + dx < 0 || x + dx >= width || y + dy < 0 || y + dy >= height) continue;
                    Dig(x + dx, y + dy);
                }
            }
        }
        //grid[x, y]
    }
    else if (grid[x, y] > 0) {
        int flAround = 0;
        for (int dx = -1; dx < 2; dx++) {
            for (int dy = -1; dy < 2; dy++) {
                if (x + dx < 0 || x + dx >= width || y + dy < 0 || y + dy >= height) continue;
                if (flags.Contains((x + dx, y + dy))) flAround++;
            }
        }

        if (flAround == grid[x, y]) {
            for (int dx = -1; dx < 2; dx++) {
                for (int dy = -1; dy < 2; dy++) {
                    if (x + dx < 0 || x + dx >= width || y + dy < 0 || y + dy >= height || grid[x+dx, y+dy] != null) continue;
                    Dig(x + dx, y + dy);
                }
            }
        }
    }
}

int[] colors = [ 30, 94, 92, 91, 35, 33, 96, 93, 95 ];
DateTime? winTime = null;
while (true) {
    if (redraw) {
        redraw = false;
        Clear();
        SetCursorPosition(ox + width-(lost || winTime != null ? 4 : 6), oy);
        WriteLine(lost ? "\e[91mYou lost\e[0m" : winTime != null ? "\e[92mYou win\e[0m" : "\e[91mCLI\e[0;1msweeper\e[0m");
        SetCursorPosition(ox, oy + 2);
        WriteLine($"╔{new string('═', width*2)}╗");
        for (int y = 0; y < height; y++) {
            SetCursorPosition(ox, oy + 3 + y);
            WriteLine($"║{new string(' ', width*2)}║");
        }
        SetCursorPosition(ox, oy + 3 + height);
        WriteLine($"╚{new string('═', width*2)}╝");
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                SetCursorPosition(ox + x * 2 + 1, oy + y + 3);
                Write(grid[x, y] == null
                    ? ". "
                    : grid[x, y] <= 0 ? "  " : $"\e[{colors[grid[x, y]!.Value]}m{grid[x, y]}\e[0m");
            }
        }

        foreach (var flag in flags) {
            SetCursorPosition(ox + flag.Item1 * 2 + 1, oy + flag.Item2 + 3);
            Write("\e[95m¶ \e[0m");
        }
        if (lost) {
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    if (!mines[x, y]) continue;
                    SetCursorPosition(ox + x * 2 + 1, oy + y + 3);
                    Write($"\e[91mX \e[0m");
                }
            }
            foreach (var flag in flags) {
                SetCursorPosition(ox + flag.Item1 * 2 + 1, oy + flag.Item2 + 3);
                Write($"\e[30;10{(mines[flag.Item1, flag.Item2] ? 2 : 1)}m¶ \e[0m");
            }
            SetCursorPosition(ox, oy + height+4);
            Write(new string(' ', WindowWidth));
            SetCursorPosition(ox, oy + height+4);
            Thread.Sleep(500);
            Write("\e[1mPress any key to exit\e[0m");
            ReadKey(true);
            Clear();
            return;
        }
        if (winTime != null) {
            SetCursorPosition(ox, oy + height+4);
            Write(new string(' ', WindowWidth));
            SetCursorPosition(ox, oy + height+4);
            Thread.Sleep(500);
            int secondss = (int)Math.Floor((winTime - gameStart).Value.TotalSeconds);
            if (timer != null) secondss = (timer - secondss).Value;
            Write($"\e[92m{secondss/60}:{secondss%60:00}\e[0m │ \e[1mPress any key to exit\e[0m");
            ReadKey(true);
            Clear();
            return;
        }
    }
    SetCursorPosition(ox, oy + height+4);
    Write(new string(' ', WindowWidth));
    SetCursorPosition(ox, oy + height+4);
    int seconds = (int)Math.Floor((DateTime.Now - gameStart).TotalSeconds);
    if (timer != null) seconds = (timer - seconds).Value;
    Write($"{cx}, {cy} │ \e[1m{minec-flags.Count}\e[0m left │ \e[1m{seconds/60}:{seconds%60:00}\e[0m");
    SetCursorPosition(ox + cx*2 + 1, oy + cy+3);
    if (KeyAvailable) {
        ConsoleKeyInfo key = ReadKey(true);
        if (key.KeyChar == 'q') {
            Clear();
            return;
        }
        if ((key.Key == ConsoleKey.UpArrow || key.KeyChar == 'w') && cy > 0) cy--;
        if ((key.Key == ConsoleKey.LeftArrow || key.KeyChar == 'a') && cx > 0) cx--;
        if ((key.Key == ConsoleKey.DownArrow || key.KeyChar == 's') && cy < height - 1) cy++;
        if ((key.Key == ConsoleKey.RightArrow || key.KeyChar == 'd') && cx < width - 1) cx++;
        if (key.Key == ConsoleKey.Enter) Dig(cx, cy);
        if (key.Key == ConsoleKey.Spacebar && grid[cx, cy] == null) {
            if (flags.Contains((cx, cy))) flags.Remove((cx, cy));
            else flags.Add((cx, cy)); redraw = true;
        }
        if (cellsRemaining == flags.Count && flags.Count == minec) {
            winTime = DateTime.Now;
            redraw = true;
        }
    }

    if (seconds <= 0 && timer != null) {
        lost = true;
        redraw = true;
    }
    Thread.Sleep(50);
}