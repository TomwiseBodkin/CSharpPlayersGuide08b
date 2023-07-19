internal class Program {
    private static void Main() {
        Game game = new Game();
        game.Run();
    }
}

public class Game {
    public void Run() {
        char inKey; // movement input key
        int FountInt;

        Coordinate pCoord = new Coordinate(0, 0);
        Player player = new Player(pCoord);

        CheckGrid checkGrid = new CheckGrid(player);

        player.nightVision = true; // 'true' to allow ability to see obstacles for testing

        while (player.alive && !player.hasWon) {
            Console.Clear();

            ColorWriter.ColorWriteLine($"You are in the room at [{player.coord.col},{player.coord.row}]. You have {player.numArrows} arrows.", ConsoleColor.Cyan);
            checkGrid.grid.DrawArray(player, checkGrid.Obstacles);

            if (!checkGrid.CheckSurroundings()) {
                continue;
            }
            FountInt = checkGrid.Obstacles.FindIndex(r => r is Fountain);

            ColorWriter.ColorWriteLine($"Type a key to issue a command ->\n w: north, a: west, s: south, d: east,\n e: enable fountain, x: shoot an arrow, q: quit, h: help", ConsoleColor.White);

            do {
                inKey = Console.ReadKey(true).KeyChar;
            } while (!"wasdqehx".Contains(inKey));
            if (player.Command is not null) {
                player.Command.Add(inKey switch {
                    _ when inKey == 'w' || inKey == 'a' || inKey == 's' || inKey == 'd' => new MoveCommand(inKey),
                    'e' => new EnableCommand(checkGrid.Obstacles[FountInt]),
                    'x' => new ShootArrow(checkGrid.Obstacles),
                    'q' => new KillCommand(),
                    'h' => new HelpCommand(),
                    _ => new KillCommand()
                });
            }
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
            do {
                ColorWriter.ColorWrite("Your choice (1-3): ", ConsoleColor.White);
                pickerStr = Console.ReadLine();
                if (pickerStr == "")
                    pickerStr = "3";
            } while (!int.TryParse(pickerStr, out pick));
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
    public void DrawArray(Player player, List<IObstacle> obstacle) {
        char[,] IconLocations = new char[gridSize, gridSize];
        Monster testLife;
        ConsoleColor[,] consoleColors = new ConsoleColor[gridSize, gridSize];

        for (int i = 0; i < gridSize; i++) {
            for (int j = 0; j < gridSize; j++) {
                IconLocations[j, i] = ' ';
                consoleColors[j, i] = ConsoleColor.Black;
            }
        }

        for (int k = 0; k < obstacle.Count; k++) {
            if (obstacle[k] is Monster) {
                testLife = (Monster)obstacle[k];
                if (testLife.alive) {
                    IconLocations[obstacle[k].coord.col, obstacle[k].coord.row] = obstacle[k].Icon();
                    if (player.nightVision)
                        consoleColors[obstacle[k].coord.col, obstacle[k].coord.row] = obstacle[k].Color();
                }
            } else {
                IconLocations[obstacle[k].coord.col, obstacle[k].coord.row] = obstacle[k].Icon();
                if (player.nightVision)
                    consoleColors[obstacle[k].coord.col, obstacle[k].coord.row] = obstacle[k].Color();
            }
        }

        IconLocations[player.coord.col, player.coord.row] = player.Icon();
        consoleColors[player.coord.col, player.coord.row] = player.Color();

        DrawLine(true);
        for (int i = 0; i < gridSize; i++) {
            Console.Write("\u2502");
            for (int j = 0; j < gridSize; j++) {
                ColorWriter.ColorWrite($" {IconLocations[j, i]} ", consoleColors[j, i]);
            }
            Console.WriteLine("\u2502");
        }
        DrawLine(false);
    }

    public void DrawLine(bool top) {
        if (top)
            Console.Write("\u250c");
        else
            Console.Write("\u2514");
        for (int i = 0; i < gridSize; i++) {
            Console.Write("\u2500\u2500\u2500");
        }
        if (top)
            Console.WriteLine("\u2510");
        else
            Console.WriteLine("\u2518");
    }
}

public class CheckGrid {
    public Grid grid { get; }
    public Player Player { get; }
    public List<IObstacle> Obstacles { get; }

    public CheckGrid(Player player) {
        Grid grid = new Grid();
        grid.ChooseGridSize();

        player.maxMove = grid.gridSize;

        Random random = new Random();
        Array impedTypes = Enum.GetValues(typeof(Impediments));
        bool repeat;

        Coordinate coordinate;

        Player = player;
        List<IObstacle> _Obstacles = new List<IObstacle>();
        coordinate = new Coordinate(0, 0);
        _Obstacles.Add(new Entrance(coordinate));
        coordinate = new Coordinate(random.Next(grid.gridSize / 2, grid.gridSize), random.Next(grid.gridSize / 2, grid.gridSize));
        _Obstacles.Add(new Fountain(coordinate));
        for (int i = 2; i < grid.gridSize + 2; i++) {
            repeat = false;
            do {
                // Initialize locations of obstacles: Maelstrom, Amarok, Pit
                // ensure they are not created at the same space
                coordinate = new Coordinate(random.Next(grid.gridSize), random.Next(grid.gridSize));
                for (int j = 0; j < i; j++) {
                    repeat = (coordinate.col == _Obstacles[j].coord.col && coordinate.row == _Obstacles[j].coord.row);
                    if (repeat) {
                        break;
                    }
                }
            } while (repeat);
            switch (impedTypes.GetValue(random.Next(impedTypes.Length))) {
                case Impediments.Maelstrom:
                    _Obstacles.Add(new Maelstrom(coordinate));
                    break;
                case Impediments.Amarok:
                    _Obstacles.Add(new Amarok(coordinate));
                    break;
                case Impediments.Pit:
                    _Obstacles.Add(new Pit(coordinate));
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
        int EntInt = Obstacles.FindIndex(r => r is Entrance);

        if (SamePoint(Player.coord, Obstacles[EntInt].coord) && Player.activateFount) {
            ColorWriter.ColorWriteLine("The fountain has been activated and you have escaped with your life!", ConsoleColor.Blue);
            Player.hasWon = true;
            return false;
        }

        // check if a maelstrom has landed on another object's location. Kill it if it has.
        for (int i = 0; i < Obstacles.Count; i++) {
            if (Obstacles[i] is Maelstrom) {
                Maelstrom testLife = (Maelstrom)Obstacles[i];
                if (testLife.alive) {
                    for (int j = 0; j < Obstacles.Count; j++) {
                        if (j != i) {
                            if (Obstacles[i].coord.col == Obstacles[j].coord.col && Obstacles[i].coord.row == Obstacles[j].coord.row) {
                                ColorWriter.ColorWriteLine($"A Maelstrom has died at [{Obstacles[i].coord.col.ToString()},{Obstacles[i].coord.row.ToString()}]", ConsoleColor.DarkRed);
                                ColorWriter.ColorWriteLine($"Killed by a {Obstacles[j].ToString()}", ConsoleColor.DarkRed);
                                Obstacles.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }
            }
        }

        for (int i = 0; i < Obstacles.Count; i++) {
            if (IsAdjacent(Player.coord, Obstacles[i].coord)) {
                Obstacles[i].Sense();
            }
            if (SamePoint(Player.coord, Obstacles[i].coord)) {
                Obstacles[i].Action(Player);
                if (Obstacles[i] is Maelstrom)
                    return false;
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
        if (coord1.col == coord2.col && coord1.row == coord2.row) {
            return true;
        }
        return false;
    }

}

public record struct Coordinate(int col, int row);

public class Player {
    public bool alive { get; set; } = true;
    public bool hasWon { get; set; } = false;
    public bool activateFount { get; set; } = false;
    public bool nightVision { get; set; }
    public int maxMove { get; set; } = 6;
    public int numArrows { get; set; } = 5;
    public Coordinate coord { get; set; }
    public Player(Coordinate coord) {
        this.coord = coord;
    }
    public List<PlayerCommand> Command { get; } = new List<PlayerCommand>();
    public void Run() {
        if (Command is not null) {
            foreach (PlayerCommand? command in Command) {
                command?.Run(this);
            }
            Command.Clear();
        } else {
            Console.WriteLine("NULL!");
            Console.ReadKey(true);
        }
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
public class MoveCommand : PlayerCommand {
    public char direction { get; set; }
    public MoveCommand(char direction) {
        this.direction = direction;
    }
    public override void Run(Player player) {
        if (player.alive) {
            Coordinate coord2 = player.coord;
            switch (direction) {
                case 'w':  // north
                    coord2.row--;
                    break;
                case 'a':  // west
                    coord2.col--;
                    break;
                case 's':  // south
                    coord2.row++;
                    break;
                case 'd':  // east
                    coord2.col++;
                    break;
                default:
                    break;
            }
            if (coord2.col >= 0 && coord2.col < player.maxMove && coord2.row >= 0 && coord2.row < player.maxMove) {
                player.coord = coord2;
            } else {
                Console.WriteLine("You cannot go through a wall! (press any key)");
                Console.ReadKey(true);
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
    IObstacle obstacle { get; }
    public EnableCommand(IObstacle obstacle) {
        this.obstacle = obstacle;
    }

    public override void Run(Player player) {
        if (player.alive && !player.activateFount) {
            if (player.coord.col == obstacle.coord.col && player.coord.row == obstacle.coord.row) {
                player.activateFount = true;
            } else {
                Console.WriteLine("You can only do that at the fountain...");
                Console.ReadKey(true);
            }
        }
    }
}

public class HelpCommand : PlayerCommand {
    public override void Run(Player player) {
        Console.Clear();
        Instructions.Printout();
        ColorWriter.ColorWriteLine("Press any key...", ConsoleColor.Cyan);
        Console.ReadKey(true);
    }
}
public class ShootArrow : PlayerCommand {
    List<IObstacle> obstacle { get; set; }
    public ShootArrow(List<IObstacle> obstacle) {
        this.obstacle = obstacle;
    }
    public override void Run(Player player) {
        if (player.numArrows > 0) {
            char inKey;
            int hitIndex = -1;
            ColorWriter.ColorWriteLine($"Type a key to pick a direction to shoot -> w: north, a: west, s: south, d: east", ConsoleColor.White);
            player.numArrows--;

            do {
                inKey = Console.ReadKey(true).KeyChar;
            } while (!"wasd".Contains(inKey));
            switch (inKey) {
                case 'w':
                    Console.WriteLine("You fire an arrow to the north");
                    for (int i = 0; i < obstacle.Count; i++) {
                        if (player.coord.col == obstacle[i].coord.col && player.coord.row - obstacle[i].coord.row == 1) {
                            hitIndex = i;
                            break;
                        }
                    }
                    break;
                case 'a':
                    Console.WriteLine("You fire an arrow to the west");
                    for (int i = 0; i < obstacle.Count; i++) {
                        if (player.coord.row == obstacle[i].coord.row && player.coord.col - obstacle[i].coord.col == 1) {
                            hitIndex = i;
                            break;
                        }
                    }
                    break;
                case 's':
                    Console.WriteLine("You fire an arrow to the south");
                    for (int i = 0; i < obstacle.Count; i++) {
                        if (player.coord.col == obstacle[i].coord.col && player.coord.row - obstacle[i].coord.row == -1) {
                            hitIndex = i;
                            break;
                        }
                    }
                    break;
                case 'd':
                    Console.WriteLine("You fire an arrow to the east");
                    for (int i = 0; i < obstacle.Count; i++) {
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
                    ColorWriter.ColorWriteLine($"You killed a {obstacle[hitIndex].GetType()}", ConsoleColor.Magenta);
                    obstacle.RemoveAt(hitIndex);
                    Console.ReadKey(true);
                } else if (obstacle[hitIndex] is Pit) {
                    ColorWriter.ColorWriteLine("Your arrow flies down into a deep pit", ConsoleColor.DarkCyan);
                    Console.ReadKey(true);
                } else if (obstacle[hitIndex] is Fountain) {
                    ColorWriter.ColorWriteLine("You hear a splash", ConsoleColor.DarkCyan);
                    Console.ReadKey(true);
                } else if (obstacle[hitIndex] is Entrance) {
                    ColorWriter.ColorWriteLine("Your arrow flies away, out of the cave, and to a new life", ConsoleColor.DarkCyan);
                    Console.ReadKey(true);
                } else {
                    ColorWriter.ColorWriteLine("Your arrow hit something, but we're not sure what", ConsoleColor.DarkCyan);
                    Console.ReadKey(true);
                }
            } else {
                ColorWriter.ColorWriteLine("Your arrow flies off into the darkness...", ConsoleColor.DarkCyan);
                Console.ReadKey(true);
            }

        } else {
            ColorWriter.ColorWriteLine("You are out of arrows!", ConsoleColor.DarkBlue);
            Console.ReadKey(true);
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
            Coordinate coordP = player.coord;
            coordP.col += 2;
            if (coordP.col >= player.maxMove) {
                coordP.col = player.maxMove - 1;
            } else if (coordP.col < 0) {
                coordP.col = 0;
            }
            coordP.row += 2;
            if (coordP.row >= player.maxMove) {
                coordP.row = player.maxMove - 1;
            } else if (coordP.row < 0) {
                coordP.row = 0;
            }
            coordM.col -= 2;
            if (coordM.col >= player.maxMove) {
                coordM.col = player.maxMove - 1;
            } else if (coordM.col < 0) {
                coordM.col = 0;
            }
            coordM.row -= 2;
            if (coordM.row >= player.maxMove) {
                coordM.row = player.maxMove - 1;
            } else if (coordM.row < 0) {
                coordM.row = 0;
            }

            this.coord = coordM;
            player.coord = coordP;
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
    public Pit(Coordinate coord) {
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
        if (!player.activateFount) {
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
    public static void Printout() {
        string instOut = """
        You enter the Cavern of Objects, a maze filled with dangerous pits and monsters, in search of 
        the Fountain of Objects. Light is visible only in the entrance, and no other light is seen 
        anywhere in the caverns. You must navigate the Caverns with your other senses.
        Find the Fountain of Objects, activate it, and return to the entrance!

        Look out for pits. You will feel a breeze if a pit is in an adjacent room. If you
        enter a room with a pit, you will die.
        
        Maelstroms are violent forces of sentient wind. Entering a room with one could transport
        you to any other location in the caverns. You will be able to hear their growling and
        groaning in nearby rooms.
        
        Amaroks roam the caverns. Encountering one is certain death, but they stink and can be
        smelled in nearby rooms.
        
        You carry with you a bow and a quiver of arrows. You can use them to shoot monsters in the
        caverns but be warned: you have a limited supply.
        """;
        ColorWriter.ColorWriteLine(instOut, ConsoleColor.Yellow);
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