﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using Terminal;

namespace TerminalControls
{
	class TerminalLineVisual : DrawingVisual
	{		
		public TerminalControl Terminal
		{ get; private set; }

		public TerminalLine Line
		{ get; private set; }

		public TerminalLineVisual(TerminalControl terminal, TerminalLine line)
		{
			this.Terminal = terminal;
			this.Line = line;

			line.RunsChanged += Line_RunsChanged;
		}

		private void Line_RunsChanged(object sender, EventArgs e)
		{
			Action action = () => Dispatcher.Invoke(redraw);
			if (Terminal.DeferChanges)
				Terminal.AddDeferChangesCallback(this, action);
			else
				action();
		}

		private void redraw()
		{
			var context = RenderOpen();

			var underlineDecoration = new TextDecorationCollection();
			underlineDecoration.Add(TextDecorations.Underline);

			var drawPoint = new System.Windows.Point(0, 0);
			foreach (var run in Line.Runs)
			{
				var ft = new FormattedText(
					run.Text,
					System.Globalization.CultureInfo.CurrentUICulture,
					Terminal.FlowDirection,
					Terminal.Typeface,
					Terminal.FontSize,
					Terminal.GetFontForegoundBrush(run.Font),
					new NumberSubstitution(),
					TextFormattingMode.Ideal
					);

				if (run.Font.Underline)
					ft.SetTextDecorations(underlineDecoration);

				context.DrawText(ft, drawPoint);
				drawPoint.X += ft.WidthIncludingTrailingWhitespace;
			}

			context.Close();
		}
	}
}
