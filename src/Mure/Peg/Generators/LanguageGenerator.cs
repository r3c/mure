using System.IO;

namespace Mure.Peg.Generators
{
	abstract class LanguageGenerator<TEmitter> : IGenerator
	{
		private readonly PegDefinition _definition;
		private readonly string _languageName;

		public LanguageGenerator(string languageName, PegDefinition definition)
		{
			_definition = definition;
			_languageName = languageName;
		}

		public void Generate(TextWriter writer)
		{
			var context = CreateContext(writer);

			EmitHeader(context, _definition.ContextType, _definition.StartIndex);

			for (var index = 0; index < _definition.States.Count; ++index)
				EmitState(context, _definition.ContextType, index);

			EmitFooter(context);
		}

		protected (PegOperation, PegAction?) GetState(int index)
		{
			var state = _definition.States[index];
			var operation = state.Operation;

			return (operation, state.Actions.TryGetValue(_languageName, out var action) ? action : null);
		}

		protected abstract TEmitter CreateContext(TextWriter writer);

		protected abstract void EmitFooter(TEmitter emitter);

		protected abstract void EmitHeader(TEmitter emitter, string contextType, int startIndex);

		protected abstract void EmitState(TEmitter emitter, string contextType, int stateIndex);
	}
}
