using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmoothListTest
{
	class DataItem
	{
		public DataItem(int counter, string name)
		{
			Counter = counter;
			Name = name;
		}

		public int Counter { get; set; }
		public string Name { get; set; }
	}
}
