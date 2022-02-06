﻿using System.IO;

namespace Mure.Peg.Generators
{
	abstract class LanguageGenerator<TContext> : IGenerator
	{
		private readonly PegDefinition _definition;
		private readonly string _languageName;

		public LanguageGenerator(string languageName, PegDefinition definition)
		{
			_definition = definition;
			_languageName = languageName;
		}

		public void Generate(TextWriter writer, int startIndex)
		{
			var context = CreateContext(writer);

			EmitHeader(context, startIndex);

			for (var index = 0; index < _definition.States.Count; ++index)
				EmitState(context, index);

			EmitFooter(context);
		}

		protected (PegOperation, PegAction?) GetState(int index)
		{
			var state = _definition.States[index];
			var operation = state.Operation;

			return (operation, state.Actions.TryGetValue(_languageName, out var action) ? action : null);
		}

		protected abstract TContext CreateContext(TextWriter writer);

		protected abstract void EmitFooter(TContext context);

		protected abstract void EmitHeader(TContext context, int startIndex);

		protected abstract void EmitState(TContext context, int stateIndex);
	}
}
