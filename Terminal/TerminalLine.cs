﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace npcook.Terminal
{
	public class TerminalLine
	{
		List<TerminalRun> runs = new List<TerminalRun>();
		public IReadOnlyList<TerminalRun> Runs
		{ get { return runs; } }

		public event EventHandler<EventArgs> RunsChanged;
		public TerminalLine()
		{ }

		int length;
		public int Length
		{ get { return length; } }

		int colCount = 0;
		public int ColCount
		{
			get { return colCount; }
			set
			{
				if (value > colCount)
				{
				}
				else if (value < colCount)
				{
					int totalIndex = 0;
					for (int i = 0; i < runs.Count; ++i)
					{
						var run = runs[i];
						if (totalIndex >= value)
						{
							runs.RemoveRange(i + 1, runs.Count - i - 1);
							break;
						}
						totalIndex += run.Text.Length;
					}
				}
				
				colCount = value;
			}
		}

		void extend(int toLength)
		{
			if (runs.Count == 0)
				runs.Add(new TerminalRun(new string(' ', toLength), new TerminalFont() { Hidden = true }));
			else
			{
				var lastRun = runs[runs.Count - 1];
				lastRun.Text = lastRun.Text + new string(' ', toLength - Length);
			}

			length = toLength;
		}

		public void InsertCharacters(int index, string chars, TerminalFont font)
		{
			if (chars.Length == 0)
				return;
			if (length < index)
				extend(index);

			int runIndex = 0;
			for (int i = 0; i < runs.Count; ++i)
			{
				var run = runs[i];

				if (runIndex + run.Text.Length >= index)
				{
					if (run.Font == font)
					{
						run.Text = run.Text.Insert(index - runIndex, chars);
					}
					else
					{
						var newRun = new TerminalRun()
						{
							Text = chars,
							Font = font
						};

						var splitRun = new TerminalRun()
						{
							Text = run.Text.Substring(index - runIndex),
							Font = run.Font
						};
						run.Text = run.Text.Substring(0, index - runIndex);
						if (run.Text.Length == 0)
						{
							run.Text = newRun.Text;
							run.Font = newRun.Font;

							if (splitRun.Text.Length > 0)
								runs.Insert(i + 1, splitRun);
						}
						else if (splitRun.Text.Length > 0)
							runs.InsertRange(i + 1, new[] { newRun, splitRun });
						else
							runs.Insert(i + 1, newRun);
					}
					break;
				}

				runIndex += run.Text.Length;
			}

			if (RunsChanged != null)
				RunsChanged(this, EventArgs.Empty);
		}

		public void DeleteCharacters(int deleteIndex, int deleteLength)
		{
			if (deleteLength == 0)
				return;

			length -= deleteLength;

			int runIndex = 0;
			for (int i = 0; i < runs.Count; ++i)
			{
				var run = runs[i];

				// Deleting the middle of a run
				if (deleteIndex >= runIndex && deleteIndex + deleteLength < runIndex + run.Text.Length)
				{
					run.Text = run.Text.Substring(0, deleteIndex - runIndex) + run.Text.Substring(deleteIndex + deleteLength - runIndex);
					if (run.Text.Length == 0)
						runs.RemoveAt(i);
					break;
				}

				// Deleting the ending of a run
				if (deleteIndex > runIndex && deleteIndex < runIndex + run.Text.Length && deleteIndex + deleteLength >= runIndex + run.Text.Length)
				{
					int remainingCount = deleteIndex - runIndex;
					deleteLength -= run.Text.Length - remainingCount;
					run.Text = run.Text.Substring(0, remainingCount);
					runIndex += run.Text.Length;
				}
				// Deleting an entire run
				else if (deleteIndex <= runIndex && deleteIndex + deleteLength >= runIndex + run.Text.Length)
				{
					runs.RemoveAt(i);
					i--;
					deleteLength -= run.Text.Length;
				}
				// Deleting the beginning of a run
				else if (runIndex >= deleteIndex)
				{
					run.Text = run.Text.Substring(deleteIndex + deleteLength - runIndex);
					break;
				}
				else
					runIndex += run.Text.Length;
			}

			if (RunsChanged != null)
				RunsChanged(this, EventArgs.Empty);
		}

		public void SetCharacters(int index, char[] chars, TerminalFont font)
		{
			SetCharacters(index, new string(chars), font);
		}

		/// <summary>
		/// Replaces the characters on this line in the range [<paramref name="index"/>, 
		/// <paramref name="index"/> + <paramref name="chars"/>.Length) with those in 
		/// <paramref name="chars"/>.
		/// 
		/// If the run at (<paramref name="index"/> - 1) or (<paramref name="index"/> + 
		/// <paramref name="chars"/>.Length) has the same font as <paramref name="font"/>,
		/// it will be extended to include the new characters.  Otherwise, a new run will
		/// be created.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="chars"></param>
		/// <param name="font"></param>
		public void SetCharacters(int index, string chars, TerminalFont font)
		{
			if (chars.Length == 0)
				return;
			if (length < index + chars.Length)
				extend(index + chars.Length);
//			if (index + chars.Length >= colCount)
//				return;
			//throw new ArgumentOutOfRangeException("index", index, "blah");

			int totalIndex = 0;
			for (int i = 0; i < runs.Count; ++i)
			{
				var run = runs[i];

				// Completely replacing an existing run
				if (index == totalIndex && chars.Length == run.Text.Length)
				{
					run.Text = chars;
					run.Font = font;

					break;
				}
				// Inside an existing run
				else if (index >= totalIndex && index < totalIndex + run.Text.Length)
				{
					int newLength = Math.Min(chars.Length, totalIndex + run.Text.Length - index);
					var newRun = new TerminalRun()
					{
						Text = chars.Substring(0, newLength),
						Font = font
					};

					var splitRun = new TerminalRun()
					{
						Text = run.Text.Substring(index - totalIndex + newLength),
						Font = run.Font
					};
					run.Text = run.Text.Substring(0, index - totalIndex);
					if (run.Text.Length == 0)
					{
						run.Text = newRun.Text;
						run.Font = newRun.Font;

						if (splitRun.Text.Length > 0)
							runs.Insert(i + 1, splitRun);
					}
					else if (splitRun.Text.Length > 0)
						runs.InsertRange(i + 1, new[] { newRun, splitRun });
					else
						runs.Insert(i + 1, newRun);

					if (newLength != chars.Length)
					{
						SetCharacters(index + newLength, chars.Substring(newLength), font);
					}

					break;
				}
				totalIndex += run.Text.Length;
			}
			
			for (int i = 0; i < runs.Count - 1; ++i)
			{
				var run1 = runs[i];
				var run2 = runs[i + 1];

				if (run1.Font == run2.Font)
				{
					run1.Text += run2.Text;
					runs.RemoveAt(i + 1);
					i--;
				}
			}

			if (RunsChanged != null)
				RunsChanged(this, EventArgs.Empty);
		}

		/// <summary>
		///	Sets the character at the specified index in this line using the given font.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="c"></param>
		/// <param name="font">Not Implemented</param>
		public void SetCharacter(int index, char c, TerminalFont font)
		{
			SetCharacters(index, new[] { c }, font);
		}

		public string GetCharacters(int index, int length)
		{
			var builder = new StringBuilder();
			int totalIndex = 0;
			foreach (var run in runs)
			{
				// Getting the middle of a run
				if (index >= totalIndex && index + length < totalIndex + run.Text.Length)
				{
					builder.Append(run.Text, index - totalIndex, length);
					break;
				}
				// Getting the ending of a run
				if (index >= totalIndex && index < totalIndex + run.Text.Length)
					builder.Append(run.Text, index - totalIndex, run.Text.Length - (index - totalIndex));
				// Getting an entire run
				else if (totalIndex >= index && index + length >= totalIndex + run.Text.Length)
					builder.Append(run.Text);
				// Deleting the beginning of a run
				else if (totalIndex >= index)
				{
					builder.Append(run.Text, 0, index + length - totalIndex);
					break;
				}

				totalIndex += run.Text.Length;
			}

			return builder.ToString();
		}
	}
}
