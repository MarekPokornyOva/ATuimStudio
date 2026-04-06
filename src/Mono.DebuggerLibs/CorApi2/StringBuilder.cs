using System;

internal sealed class StringBuilder
{
	char[] _buffer;
	public StringBuilder(int length)
	{
		_buffer = new char[length];
	}

	public static implicit operator char[](StringBuilder sb) => sb._buffer;

	public int Capacity => _buffer.Length;
	public int Length
	{
		get => _buffer.Length;
		internal set => Array.Resize(ref _buffer, value);
	}

	public override string ToString()
		//=> new string(_buffer, 0, _buffer.Length - 1);
		=> _buffer[_buffer.Length - 1] == '\0'
			? new string(_buffer, 0, _buffer.Length - 1)
			: throw new InvalidOperationException("The text does not end with null char.");
}
