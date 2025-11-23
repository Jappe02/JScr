namespace JScr.Utils;

public readonly struct Range
{
    public Range(Position fromTo)
    {
        this.from = fromTo;
        this.to = fromTo;
    }

    public Range(Position from, Position to)
    {
        this.from = from;
        this.to = to;
    }

    public readonly Position from, to;
}