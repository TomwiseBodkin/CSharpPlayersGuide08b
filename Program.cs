internal class Program {
    private static void Main() {
        Game game = new Game();
        game.Run();
    }
}

public class Game {
    public void Run() {
        char inKey; // movement input key

        Coordinate pCoord = new Coordinate(0,0);
        Player player = new Player(pCoord);

        CheckGrid checkGrid = new CheckGrid(player);

        while (player.alive && !player.hasWon) {
            Console.Clear();

            checkGrid.grid.DrawArray(player, checkGrid.Obstacles);
            Console.WriteLine($"You are in the room at [{player.coord.row},{player.coord.col}]. You have {player.numArrows} arrows.");

            if (!checkGrid.CheckSurroundings()) { 
                continue; 
            }

            ColorWriter.ColorWriteLine($"Type a key to issue a command ->\n w: north, a: west, s: south, d: east,\n e: enable fountain, x: shoot an arrow, q: quit, h: help", ConsoleColor.White);

            do {
                inKey = Console.ReadKey().KeyChar;
            } while (!"wasdqehx".Contains(inKey));
            player.Command[0] = inKey switch {
                'w' => new NorthCommand(),
                's' => new SouthCommand(),
                'a' => new WestCommand(),
                'd' => new EastCommand(),
                'e' => new EnableCommand(),
                'x' => new ShootArrow(checkGrid.Obstacles),
                'k' => new KillCommand(),
                'h' => new HelpCommand(),
                 _ => new KillCommand()
            };
            player.Run();
            Console.WriteLine("");
        }
    }
}



public class Grid {
    public int gridSize { get; set; } = 6;
    public void ChooseGridSize() {
        int pick;
        string? pickerStr;
        Instructions.Printout();
        Console.WriteLine("Decide which size of region you want to explore:");
        Console.WriteLine("1 - 6x6");
        Console.WriteLine("2 - 9x9");
        Console.WriteLine("3 - 12x12");
        do {
            ColorWriter.ColorWrite("Your choice (1-3): ", ConsoleColor.White);
            pickerStr = Console.ReadLine();
            if (pickerStr == "")
                pickerStr = "3";
            while (!int.TryParse(pickerStr, out pick)) {
                ColorWriter.ColorWrite("Your choice (1-3): ", ConsoleColor.White);
                pickerStr = Console.ReadLine();
            }
        } while (pick < 1 || pick > 3);

        switch (pick) {
            case 1:
                gridSize = 6;
                break; 
            case 2:
                gridSize = 9;
                break;
            case 3:
                gridSize = 12;
                break;
            default:
                gridSize = 9;
                break;
        }
        Console.Clear();
    }
    public void DrawArray(Player player, IObstacle[] obstacle) {
        char[,] IconLocations = new char[gridSize,gridSize];
        Monster testLife;
        ConsoleColor[,] consoleColors = new ConsoleColor[gridSize,gridSize]; 

        for (int i = 0; i < gridSize; i++) {
            for (int j = 0; j < gridSize; j++) {
                IconLocations[i, j] = ' ';
                consoleColors[i, j] = ConsoleColor.Black;
            }
        }

        for (int k = 0; k < obstacle.Length; k++) {
            if (obstacle[k] is Monster) {
                testLife = (Monster)obstacle[k];
                if (testLife.alive) {
                    IconLocations[obstacle[k].coord.row, obstacle[k].coord.col] = obstacle[k].Icon();
                    // consoleColors[obstacle[k].coord.row, obstacle[k].coord.col] = obstacle[k].Color();
                }
            } else {
                IconLocations[obstacle[k].coord.row, obstacle[k].coord.col] = obstacle[k].Icon();
                // consoleColors[obstacle[k].coord.row, obstacle[k].coord.col] = obstacle[k].Color();
            }
        }

        IconLocations[player.coord.row, player.coord.col] = player.Icon();
        consoleColors[player.coord.row, player.coord.col] = player.Color();

        DrawLine();
        for (int i = 0; i < gridSize; i++) {
            Console.Write("|");
            for (int j = 0; j < gridSize; j++) {
                ColorWriter.ColorWrite($" {IconLocations[i, j]} ", consoleColors[i, j]);
            }
            Console.WriteLine("|");
        }
        DrawLine();
    }

    public void DrawLine() {
        for (int i = 0; i < gridSize; i++) {
            Console.Write("---");
        }
        Console.WriteLine("--");
    }
}

public class CheckGrid {
    public Grid grid { get; }
    public Player Player { get; }
    public IObstacle[] Obstacles { get; }

    public CheckGrid(Player player) {
        Grid grid = new Grid();
        grid.ChooseGridSize();

        player.maxMove = grid.gridSize;

        Random random = new Random();
        Array values = Enum.GetValues(typeof(Impediments));
        bool repeat;

        Coordinate coordinate;

        Player = player;
        IObstacle[] _Obstacles = new IObstacle[grid.gridSize + 3];
        coordinate = new Coordinate(0,0);
        _Obstacles[0] = new Entrance(coordinate);
        coordinate = new Coordinate(random.Next(grid.gridSize / 2, grid.gridSize), random.Next(grid.gridSize / 2, grid.gridSize));
        _Obstacles[1] = new Fountain(coordinate);
        for (int i = 2; i < _Obstacles.Length; i++) {
                repeat = false;
            do {
                // Initialize locations of obstacles: Maelstrom, Amarok, Pit
                coordinate = new Coordinate(random.Next(grid.gridSize), random.Next(grid.gridSize));
                for (int j = 0; j < i; j++) {
                    repeat = (coordinate.row == _Obstacles[j].coord.row && coordinate.col == _Obstacles[j].coord.col);
                    if (repeat) {
                        break;
                    }
                }
            } while (repeat);
            switch (values.GetValue(random.Next(values.Length))) {
                case Impediments.Maelstrom:
                    _Obstacles[i] = new Maelstrom(coordinate);
                    break;
                case Impediments.Amarok:
                    _Obstacles[i] = new Amarok(coordinate);
                    break;
                case Impediments.Pit:
                    _Obstacles[i] = new Pit(coordinate);
                    break;
                default:
                    break;
            }
        }
        this.Player = player;
        this.grid = grid;
        this.Obstacles = _Obstacles;
    }

    public bool CheckSurroundings() {
        if (Player.moveNum > 0) {
            if (SamePoint(Player.coord, Obstacles[0].coord) && Player.activateFount) {
                ColorWriter.ColorWriteLine("The fountain has been activated and you have escaped with your life!", ConsoleColor.Blue);
                Player.hasWon = true;
                return false;
            }
        }

        for (int i = 0; i < Obstacles.Length; i++) {
            if (IsAdjacent(Player.coord, Obstacles[i].coord)) {
                Obstacles[i].Sense();
            }
            if (SamePoint(Player.coord, Obstacles[i].coord)) {
                Obstacles[i].Action(Player);
            }
        }
        if (!Player.alive) { 
            return false; 
        } else {
            return true;
        }
    }
    public bool IsAdjacent(Coordinate coord1, Coordinate coord2) {
        if ((Math.Abs(coord1.col - coord2.col) == 1 && Math.Abs(coord1.row - coord2.row) <= 1) ||
                    (Math.Abs(coord1.col - coord2.col) <= 1 && Math.Abs(coord1.row - coord2.row) == 1)) {
            return true;

        }
        return false;
    }
    public bool SamePoint(Coordinate coord1, Coordinate coord2) {
        if (coord1.row == coord2.row && coord1.col == coord2.col) {
            return true;
        }
        return false;
    }

}

public record struct Coordinate(int row, int col);

public class Player {
    public bool alive { get; set; } = true;
    public bool hasWon { get; set; } = false;
    public bool foundFount { get; set; } = false;
    public bool activateFount { get; set; } = false;
    public int moveNum { get; set; } = 0;
    public int maxMove { get; set; } = 6;
    public int numArrows { get; set; } = 5;
    public Coordinate coord { get; set; }
    public Player(Coordinate coord) {
        this.coord = coord;
    }
    public PlayerCommand?[] Command { get; } = new PlayerCommand?[12];
    public void Run() {
        foreach (PlayerCommand? command in Command) {
            command?.Run(this);
        }
        moveNum++;
    }
    public char Icon() {
        return '@';
    }
    public ConsoleColor Color() { 
        return ConsoleColor.Green; 
    }
}

public interface IObstacle {
    public Coordinate coord { get; set; }
    public void Sense();
    public void Action(Player player);
    public char Icon();
    public ConsoleColor Color();
}

public abstract class Monster : IObstacle {
    public bool alive { get; set; } = true;
    public bool detected { get; set; } = false;
    public Coordinate coord { get; set; }
    public abstract void Sense();
    public abstract void Action(Player player);
    public abstract char Icon();
    public abstract ConsoleColor Color();

}


public abstract class PlayerCommand {
    public abstract void Run(Player player);
}
public class NorthCommand : PlayerCommand {
    public override void Run(Player player) {
        if (player.alive) {
            Coordinate coord2 = player.coord;
            coord2.row--;
            if (coord2.row >= 0 && coord2.row < player.maxMove) {
                player.coord = coord2;
            } else {
                Console.WriteLine("You cannot go through a wall! (press any key)");
                Console.ReadKey();
            }
        }
    }
}
public class SouthCommand : PlayerCommand {
    public override void Run(Player player) {
        if (player.alive) {
            Coordinate coord2 = player.coord;
            coord2.row++;
            if (coord2.row >= 0 && coord2.row < player.maxMove) {
                player.coord = coord2;
            } else {
                Console.WriteLine("You cannot go through a wall! (press any key)");
                Console.ReadKey();
            }
        }
    }
}
public class EastCommand : PlayerCommand {
    public override void Run(Player player) {
        if (player.alive) {
            Coordinate coord2 = player.coord;
            coord2.col++;
            if (coord2.col >= 0 && coord2.col < player.maxMove) {
                player.coord = coord2;
            } else {
                Console.WriteLine("You cannot go through a wall! (press any key)");
                Console.ReadKey();
            }
        }
    }
}
public class WestCommand : PlayerCommand {
    public override void Run(Player player) {
        if (player.alive) {
            Coordinate coord2 = player.coord;
            coord2.col--;
            if (coord2.col >= 0 && coord2.col < player.maxMove) {
                player.coord = coord2;
            } else {
                Console.WriteLine("You cannot go through a wall! (press any key)");
                Console.ReadKey();
            }
        }
    }
}
public class KillCommand : PlayerCommand {
    public override void Run(Player player) {
        if (player.alive) {
            player.alive = false;
        }
    }
}

public class EnableCommand : PlayerCommand {
    public override void Run(Player player) {
        if (player.alive && player.foundFount) {
            player.activateFount = true;
        } else {
            Console.WriteLine("You cannot do that until you find the fountain...");
            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }
}

public class HelpCommand : PlayerCommand {
    public override void Run(Player player) {
        Console.Clear();
        Instructions.Printout();
        ColorWriter.ColorWriteLine("Press any key...", ConsoleColor.Cyan);
        Console.ReadKey();
    }
}
public class ShootArrow : PlayerCommand {
    IObstacle[] obstacle { get; set; }
    public ShootArrow(IObstacle[] obstacle) {
        this.obstacle = obstacle;
    }
    public override void Run(Player player) {
        if (player.numArrows > 0) {
            char inKey;
            Monster testLife;
            int hitIndex = -1;
            ColorWriter.ColorWriteLine($"Type a key to pick a direction to shoot -> w: north, a: west, s: south, d: east", ConsoleColor.White);
            player.numArrows--;
            Console.WriteLine(obstacle[0].ToString());

            do {
                inKey = Console.ReadKey().KeyChar;
            } while (!"wasd".Contains(inKey));
            switch (inKey) {
                case 'w':
                    Console.WriteLine("You fire an arrow to the north");
                    for (int i = 0; i < obstacle.Length; i++) { 
                        if (player.coord.col == obstacle[i].coord.col && player.coord.row - obstacle[i].coord.row == 1) {
                            hitIndex = i;
                            break;
                        }
                    }
                    break;
                case 'a':
                    Console.WriteLine("You fire an arrow to the west");
                    for (int i = 0; i < obstacle.Length; i++) {
                        if (player.coord.row == obstacle[i].coord.row && player.coord.col - obstacle[i].coord.col == 1) {
                            hitIndex = i;
                            break;
                        }
                    }
                    break;
                case 's':
                    Console.WriteLine("You fire an arrow to the south");
                    for (int i = 0; i < obstacle.Length; i++) {
                        if (player.coord.col == obstacle[i].coord.col && player.coord.row - obstacle[i].coord.row == -1) {
                            hitIndex = i;
                            break;
                        }
                    }
                    break;
                case 'd':
                    Console.WriteLine("You fire an arrow to the east");
                    for (int i = 0; i < obstacle.Length; i++) {
                        if (player.coord.row == obstacle[i].coord.row && player.coord.col - obstacle[i].coord.col == -1) {
                            hitIndex = i;
                            break;
                        }
                    }
                    break;
                default:
                    break;
            };
            if (hitIndex >= 0) {
                if (obstacle[hitIndex] is Monster) {
                    testLife = (Monster)obstacle[hitIndex];
                    testLife.alive = false;
                    obstacle[hitIndex] = testLife;
                    ColorWriter.ColorWriteLine($"You killed a {obstacle[hitIndex].GetType()}", ConsoleColor.Magenta);
                    Console.ReadKey();
                } else {
                    ColorWriter.ColorWriteLine("Your arrow flies down into a deep pit", ConsoleColor.DarkCyan);
                    Console.ReadKey();
                }
            } else {
                ColorWriter.ColorWriteLine("Your arrow flies off into the darkness...",ConsoleColor.DarkCyan);
                Console.ReadKey();
            }

        } else {
            ColorWriter.ColorWriteLine("You are out of arrows! (press any key)", ConsoleColor.DarkBlue);
            Console.ReadKey();
        }
    }
}

public class Maelstrom : Monster {
    public Maelstrom(Coordinate coord) {
        this.coord = coord;
    }
    public override void Sense() {
        if (this.alive) {
            ColorWriter.ColorWriteLine("You hear the growling and groaning of a maelstrom nearby...", ConsoleColor.DarkYellow);
        }
    }
    public override void Action(Player player) {
        if (this.alive) {
            ColorWriter.ColorWriteLine("You have encountered a maelstrom and have been swept away to another room!", ConsoleColor.Cyan);
            Coordinate coordM = this.coord;
            new NorthCommand();
            new NorthCommand();
            new WestCommand();
            new WestCommand();
            player.Run();
            coordM.row -= 2;
            if (coordM.row >= player.maxMove) {
                coordM.row = player.maxMove - 1;
            } else if (coordM.row < 0) {
                coordM.row = 0;
            }
            coordM.col -= 2;
            if (coordM.col >= player.maxMove) {
                coordM.col = player.maxMove - 1;
            } else if (coordM.col < 0) {
                coordM.col = 0;
            }

            this.coord = coordM;
        }
    }
    public override char Icon() {
        return 'm';
    }
    public override ConsoleColor Color() {
        return ConsoleColor.Red;
    }

}

public class Pit : IObstacle {
    public Pit (Coordinate coord) {
        this.coord = coord;
    }

    public Coordinate coord { get; set; }
    public void Sense() {
        ColorWriter.ColorWriteLine("You feel a draft. There is a pit in a nearby room...", ConsoleColor.DarkYellow);
    }
    public void Action(Player player) {
        ColorWriter.ColorWriteLine("You have fallen into a pit and died!", ConsoleColor.Magenta);
        player.alive = false;
    }
    public char Icon() {
        return 'p';
    }
    public ConsoleColor Color() {
        return ConsoleColor.DarkRed;
    }

}
public class Entrance : IObstacle {
    public Entrance(Coordinate coord) {
        this.coord = coord;
    }

    public Coordinate coord { get; set; }
    public void Sense() { }
    public void Action(Player player) {
        if (player.activateFount) {
            ColorWriter.ColorWriteLine("The fountain has been activated and you have escaped with your life!", ConsoleColor.Yellow);
        } else if (!player.activateFount) {
            ColorWriter.ColorWriteLine("You see light in this room coming from outside the cavern. This is the entrance.", ConsoleColor.White);
        }
    }
    public char Icon() {
        return 'e';
    }
    public ConsoleColor Color() {
        return ConsoleColor.Yellow;
    }
}


public class Fountain : IObstacle {
    public Fountain(Coordinate coord) {
        this.coord = coord;
    }

    public Coordinate coord { get; set; }
    public void Sense() { }
    public void Action(Player player) {
        if (player.activateFount) {
            ColorWriter.ColorWriteLine("You hear the rushing waters from the fountain. It has been reactivated!", ConsoleColor.Blue);
        } else if (!player.activateFount) {
            ColorWriter.ColorWriteLine("You hear water dripping in this room. The fountain is here!", ConsoleColor.DarkCyan);
            player.foundFount = true;
        }
    }
    public char Icon() {
        return 'F';
    }
    public ConsoleColor Color() {
        return ConsoleColor.Blue;
    }
}

public class Amarok : Monster { 
    public Amarok(Coordinate coord) {
        this.coord = coord;
    }
    public override void Sense() {
        if (this.alive) {
            ColorWriter.ColorWriteLine("You can smell the stench of an Amarok nearby...", ConsoleColor.DarkYellow);
        }
    }
    public override void Action(Player player) {
        if (this.alive) {
            ColorWriter.ColorWriteLine("You have encountered an amarok. It ate you for dinner...", ConsoleColor.Magenta);
            player.alive = false;
        }
    }
    public override char Icon() {
        return 'a';
    }
    public override ConsoleColor Color() {
        return ConsoleColor.Red;
    }
}

public static class Instructions {
    public static void Printout () {
        Console.WriteLine("You enter the Cavern of Objects, a maze filled with dangerous pits, in search");
        Console.WriteLine("of the Fountain of Objects.");
        Console.WriteLine("Light is visible only in the entrance, and no other light is seen anywhere in the caverns.");
        Console.WriteLine("You must navigate the Caverns with your other senses.");
        Console.WriteLine("Find the Fountain of Objects, activate it, and return to the entrance.");
        Console.WriteLine("Look out for pits. You will feel a breeze if a pit is in an adjacent room. If you");
        Console.WriteLine("enter a room with a pit, you will die.");
        Console.WriteLine("Maelstroms are violent forces of sentient wind. Entering a room with one could transport");
        Console.WriteLine("you to any other location in the caverns. You will be able to hear their growling and");
        Console.WriteLine("groaning in nearby rooms.");
        Console.WriteLine("Amaroks roam the caverns. Encountering one is certain death, but they stink and can be");
        Console.WriteLine("smelled in nearby rooms.");
        // Console.WriteLine("You carry with you a bow and a quiver of arrows. You can use them to shoot monsters in the");
        // Console.WriteLine("caverns but be warned: you have a limited supply.");
        Console.WriteLine("");
    }
}

public static class ColorWriter { 
    public static void ColorWriteLine(string LineOut, ConsoleColor Color) {
        Console.ForegroundColor = Color;
        Console.WriteLine(LineOut);
        Console.ForegroundColor = ConsoleColor.Gray;
    }
    public static void ColorWrite(string LineOut, ConsoleColor Color) {
        Console.ForegroundColor = Color;
        Console.Write(LineOut);
        Console.ForegroundColor = ConsoleColor.Gray;
    }

}

public enum Impediments { Pit, Maelstrom, Amarok }