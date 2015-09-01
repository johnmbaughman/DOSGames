using System;
using System.Collections.Generic;

namespace Eliza
{
	public class Keywords : List<string>
	{
		public Keywords ()
		{
			// Refactor to something FAR better...
			this.Add ("CAN YOU ");
			this.Add ("CAN I ");
			this.Add ("YOU ARE ");
			this.Add ("YOU'RE ");
			this.Add ("I DON'T ");
			this.Add ("I FEEL ");
			this.Add ("WHY DON'T YOU ");
			this.Add ("WHY CAN'T I ");
			this.Add ("ARE YOU ");
			this.Add ("I CAN'T ");
			this.Add ("I AM ");
			this.Add ("I'M ");
			this.Add ("YOU ");
			this.Add ("I WANT ");
			this.Add ("WHAT ");
			this.Add ("HOW ");
			this.Add ("WHO ");
			this.Add ("WHERE ");
			this.Add ("WHEN ");
			this.Add ("WHY ");
			this.Add ("NAME ");
			this.Add ("CAUSE ");
			this.Add ("SORRY ");
			this.Add ("DREAM ");
			this.Add ("HELLO ");
			this.Add ("HI ");
			this.Add ("MAYBE ");
			this.Add ("NO");
			this.Add ("YOUR ");
			this.Add ("ALWAYS ");
			this.Add ("THINK ");
			this.Add ("ALIKE ");
			this.Add ("YES ");
			this.Add ("FRIEND ");
			this.Add ("COMPUTER");
			this.Add ("NOKEYFOUND");
		}
	}
}

