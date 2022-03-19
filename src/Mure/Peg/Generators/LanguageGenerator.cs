using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mure.Peg.Generators
{
	abstract class LanguageGenerator<TEmitter> : IGenerator
	{
		private readonly PegConfiguration _configuration;
		private readonly string _languageName;
		private readonly string _startKey;
		private readonly IReadOnlyDictionary<string, PegState> _states;

		public LanguageGenerator(string languageName, PegDefinition definition)
		{
			if (definition.States.Count < 1)
				throw new ArgumentOutOfRangeException(nameof(definition), "definition must contain at least one state");

			_configuration = definition.Configurations.TryGetValue(languageName, out var configuration) ? configuration : new PegConfiguration(null, null, null);
			_languageName = languageName;
			_startKey = definition.States[0].Key;
			_states = definition.States.GroupBy(state => state.Key).ToDictionary(group => group.Key, group => group.First());
		}

		public PegError? Generate(TextWriter writer)
		{
			var context = CreateContext(writer);
			var error = EmitHeader(context, _configuration, _startKey);

			if (error is not null)
				return error;

			foreach (var state in _states)
			{
				error = EmitState(context, _configuration, state.Key);

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

		protected abstract PegError? EmitHeader(TEmitter emitter, PegConfiguration configuration, string startKey);

		protected abstract PegError? EmitState(TEmitter emitter, PegConfiguration configuration, string key);
	}
}
