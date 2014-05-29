using System;
using System.Collections.Generic;
using System.Linq;

namespace Unclassified.Util
{
	/// <summary>
	/// Provides options and arguments parsing from command line arguments or a single string.
	/// </summary>
	internal class CommandLineHelper
	{
		private string[] args;
		private List<Option> options = new List<Option>();
		private List<Argument> parsedArguments = new List<Argument>();

		/// <summary>
		/// Gets or sets a value indicating whether the option names are case-sensitive. (Default:
		/// false)
		/// </summary>
		public bool IsCaseSensitive { get; set; }

		/// <summary>
		/// Reads the command line arguments from a single string.
		/// </summary>
		/// <param name="argsString">The string that contains the entire command line.</param>
		public void ReadArgs(string argsString)
		{
			args = argsString.Split(' ');
			// TODO
			// http://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp
		}

		/// <summary>
		/// Registers a named option without additional parameters.
		/// </summary>
		/// <param name="name">The option name.</param>
		/// <returns>The option instance.</returns>
		public Option RegisterOption(string name)
		{
			return RegisterOption(name, 0);
		}

		/// <summary>
		/// Registers a named option.
		/// </summary>
		/// <param name="name">The option name.</param>
		/// <param name="parameterCount">The number of additional parameters for this option.</param>
		/// <returns>The option instance.</returns>
		public Option RegisterOption(string name, int parameterCount)
		{
			Option option = new Option(name, parameterCount);
			options.Add(option);
			return option;
		}

		/// <summary>
		/// Parses all command line arguments.
		/// </summary>
		public void Parse()
		{
			// Use args of the current process if no other source was given
			if (args == null)
			{
				args = Environment.GetCommandLineArgs();
			}

			parsedArguments.Clear();
			StringComparison strComp = IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
			var aw = new EnumerableWalker<string>(args);
			bool optMode = true;
			foreach (string arg in aw)
			{
				if (arg == "--")
				{
					optMode = false;
				}
				else if (optMode && (arg.StartsWith("/") || arg.StartsWith("-")))
				{
					string optName = arg.Substring(arg.StartsWith("--") ? 2 : 1);
					var option = options.FirstOrDefault(o => o.Names.Any(n => n.Equals(optName, strComp)));
					if (option == null)
					{
						throw new Exception("Invalid option: " + arg);
					}
					if (option.IsSingle && option.IsSet)
					{
						throw new Exception("Option cannot be set multiple times: " + arg);
					}
					string[] values = new string[option.ParameterCount];
					for (int i = 0; i < option.ParameterCount; i++)
					{
						values[i] = aw.GetNext();
						if (values[i] == null)
						{
							throw new Exception("Missing argument " + (i + 1) + " for option: " + arg);
						}
					}
					var argument = new Argument(option, values);

					option.IsSet = true;
					option.Argument = argument;

					if (option.Action != null)
					{
						option.Action(argument);
					}
					else
					{
						parsedArguments.Add(argument);
					}
				}
				else
				{
					parsedArguments.Add(new Argument(null, new[] { arg }));
				}
			}

			var missingOption = options.FirstOrDefault(o => o.IsRequired && !o.IsSet);
			if (missingOption != null)
			{
				throw new Exception("Missing required option: /" + missingOption.Names[0]);
			}
		}

		/// <summary>
		/// Gets the parsed arguments.
		/// </summary>
		/// <remarks>
		/// To avoid exceptions thrown, call the <see cref="Parse"/> method in advance for
		/// exception handling.
		/// </remarks>
		public Argument[] Arguments
		{
			get
			{
				if (parsedArguments == null)
				{
					Parse();
				}
				return parsedArguments.ToArray();
			}
		}

		/// <summary>
		/// Represents a named option.
		/// </summary>
		public class Option
		{
			/// <summary>
			/// Initialises a new instance of the <see cref="Option"/> class.
			/// </summary>
			/// <param name="name">The primary name of the option.</param>
			/// <param name="parameterCount">The number of additional parameters for this option.</param>
			internal Option(string name, int parameterCount)
			{
				this.Names = new List<string>() { name };
				this.ParameterCount = parameterCount;
			}

			/// <summary>
			/// Gets the names of this option.
			/// </summary>
			public List<string> Names { get; private set; }

			/// <summary>
			/// Gets the number of additional parameters for this option.
			/// </summary>
			public int ParameterCount { get; private set; }

			/// <summary>
			/// Gets a value indicating whether this option is required.
			/// </summary>
			public bool IsRequired { get; private set; }

			/// <summary>
			/// Gets a value indicating whether this option can only be specified once.
			/// </summary>
			public bool IsSingle { get; private set; }

			/// <summary>
			/// Gets the action to invoke when the option is set.
			/// </summary>
			public Action<Argument> Action { get; private set; }

			/// <summary>
			/// Gets a value indicating whether this option is set in the command line.
			/// </summary>
			public bool IsSet { get; internal set; }

			/// <summary>
			/// Gets the <see cref="Argument"/> instance that contains additional parameters set
			/// for this option.
			/// </summary>
			public Argument Argument { get; internal set; }

			/// <summary>
			/// Gets the value of the <see cref="Argument"/> instance for this option.
			/// </summary>
			public string Value { get { return Argument != null ? Argument.Value : null; } }

			/// <summary>
			/// Sets alias names for this option.
			/// </summary>
			/// <param name="names">The alias names for this option.</param>
			/// <returns>The current <see cref="Option"/> instance.</returns>
			public Option Alias(params string[] names)
			{
				this.Names.AddRange(names);
				return this;
			}

			/// <summary>
			/// Marks this option as required. If a required option is not set in the command line,
			/// an exception is thrown on parsing.
			/// </summary>
			/// <returns>The current <see cref="Option"/> instance.</returns>
			public Option Required()
			{
				this.IsRequired = true;
				return this;
			}

			/// <summary>
			/// Marks this option as single. If a single option is set multiple times in the
			/// command line, an exception is thrown on parsing.
			/// </summary>
			/// <returns>The current <see cref="Option"/> instance.</returns>
			public Option Single()
			{
				this.IsSingle = true;
				return this;
			}

			/// <summary>
			/// Sets the action to invoke when the option is set.
			/// </summary>
			/// <param name="action">The action to invoke when the option is set.</param>
			/// <returns>The current <see cref="Option"/> instance.</returns>
			public Option Do(Action<Argument> action)
			{
				this.Action = action;
				return this;
			}
		}

		/// <summary>
		/// Represents a logical argument in the command line. Options with their additional
		/// parameters are combined in one argument.
		/// </summary>
		public class Argument
		{
			/// <summary>
			/// Initialises a new instance of the <see cref="Argument"/> class.
			/// </summary>
			/// <param name="option">The <see cref="Option"/> that is set in this argument; or null.</param>
			/// <param name="values">The additional parameter values for the option; or the argument value.</param>
			internal Argument(Option option, string[] values)
			{
				this.Option = option;
				this.Values = values;
			}

			/// <summary>
			/// Gets the <see cref="Option"/> that is set in this argument; or null.
			/// </summary>
			public Option Option { get; private set; }

			/// <summary>
			/// Gets the additional parameter values for the option; or the argument value.
			/// </summary>
			public string[] Values { get; private set; }

			/// <summary>
			/// Gets the first item of <see cref="Values"/>; or null.
			/// </summary>
			public string Value { get { return Values.Length > 0 ? Values[0] : null; } }
		}
	}
}
