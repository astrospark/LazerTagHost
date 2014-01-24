using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LazerTagHostLibrary
{
	public class LazerTagString
	{
		public LazerTagString()
		{

		}

		public LazerTagString(string text)
		{
			Text = text;
		}

		public LazerTagString(byte[] text)
		{
			Text = BytesToString(text);
		}

		public bool IsEmpty()
		{
			return string.IsNullOrWhiteSpace(Text);
		}

		public override string ToString()
		{
			return Text;
		}

		public byte[] GetBytes(int maxLength, bool addPadding = false, bool addNullTerminator = false)
		{
			return StringToBytes(Text, maxLength, addPadding, addNullTerminator);
		}

		public Signature[] GetSignatures(int maxLength, bool addPadding = false, bool addNullTerminator = false)
		{
			var bytes = GetBytes(maxLength, addPadding, addNullTerminator);
			var signatures = new List<Signature>();
			foreach (var character in bytes)
			{
				signatures.Add(new Signature(SignatureType.Data, character));
			}
			return signatures.ToArray();
		}

		// LazerTagString -> string
		public static implicit operator string(LazerTagString input)
		{
			return input.ToString();
		}

		// string -> LazerTagString
		public static explicit operator LazerTagString(string input)
		{
			return new LazerTagString(input);
		}

		// byte[] to LazerTagString
		public static implicit operator LazerTagString(byte[] input)
		{
			return new LazerTagString(input);
		}

		private string _text;
		public string Text
		{
			get { return _text; }
			set { _text = Sanitize(value); }
		}

		private static string Sanitize(string text)
		{
			// Remove invalid characters
			text = Regex.Replace(text, @"[^0-9a-zA-Z !\-\.:\?_~]", string.Empty);

			// Capitalize the text
			text = text.ToUpperInvariant();

			return text;
		}

		private static string BytesToString(byte[] inputBytes)
		{
			// Get the length of the input
			var length = inputBytes.Length;

			// Find the first null terminator
			var nullIndex = Array.FindIndex(inputBytes, 0, x => x == 0);

			// If a null terminator was found, adjust the length accordingly
			if (nullIndex != -1) length = nullIndex + 1;

			// Convert the byte array to a string
			var outputString = Encoding.ASCII.GetString(inputBytes, 0, length);

			// Trim leading and trailing whitespace
			outputString = outputString.Trim();

			return outputString;
		}

		private static byte[] StringToBytes(string inputString, int maxLength, bool addPadding, bool addNullTerminator)
		{
			// Add padding if required
			if (addPadding) inputString = inputString.PadRight(maxLength);

			// Trim down to _maxLength
			inputString = inputString.Substring(0, maxLength);

			// Get the final length
			var finalLength = inputString.Length;
			if (addNullTerminator) finalLength++;

			// Create the output array
			var outputBytes = new byte[finalLength];

			// Copy the text into the array
			var inputChars = inputString.ToCharArray(0, inputString.Length);
			var inputBytes = Encoding.ASCII.GetBytes(inputChars, 0, inputString.Length);
			inputBytes.CopyTo(outputBytes, 0);

			// Add the null terminator
			if (addNullTerminator) outputBytes[finalLength - 1] = 0;

			return outputBytes;
		}
	}
}
