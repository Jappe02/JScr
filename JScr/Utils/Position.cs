namespace JScr.Utils;

public readonly struct Position
{
    public Position(uint line, uint col)
    {
        this.line = line;
        this.col = col;
    }

    public readonly uint line, col;
}