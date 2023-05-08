namespace Mure;

public readonly struct Match<TValue>
{
	public readonly string Capture;
	public readonly TValue Value;

	public Match(TValue value, string capture)
	{
		Capture = capture;
		Value = value;
	}
}