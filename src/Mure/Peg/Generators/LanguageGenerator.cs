using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mure.Peg.Generators
{
	abstract class LanguageGenerator<TEmitter> : IGenerator
	{
		private readonly string _contextType;
		private readonly string _languageName;
		private readonly string _startKey;
		private readonly IReadOnlyDictionary<string, PegState> _states;

		public LanguageGenerator(string languageName, PegDefinition definition)
		{
			_contextType = definition.ContextType;
			_languageName = languageName;
			_startKey = definition.StartKey;
			_states = definition.States.GroupBy(state => state.Key).ToDictionary(group => group.Key, group => group.First());
		}

		public PegError? Generate(TextWriter writer)
		{
			var context = CreateContext(writer);
			var error = EmitHeader(context, _contextType, _startKey);

			if (error is not null)
				return error;

			foreach (var state in _states)
			{
				error = EmitState(context, _contextType, state.Key);

				if (error is not null)
					return error;
			}

			return EmitFooter(context);
		}

		protected bool TryGetState(string key, out PegOperation operation, out PegAction? action)
		{
			if (!_states.TryGetValue(key, out var state))
			{
				operation = default;
				action = default;

				return false;
			}

			operation = state.Operation;
			action = state.Actions.TryGetValue(_languageName, out var actionValue) ? actionValue : null;

			return true;
		}

		protected abstract TEmitter CreateContext(TextWriter writer);

		protected abstract PegError? EmitFooter(TEmitter emitter);

		protected abstract PegError? EmitHeader(TEmitter emitter, string contextType, string startKey);

		protected abstract PegError? EmitState(TEmitter emitter, string contextType, string startKey);
	}
}
