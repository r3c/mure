using System;
using Mure.Automata;
using NUnit.Framework;

namespace Mure.Test.Automata
{
	public class NonDeterministicAutomataTester
	{
		[Test]
		public void ConvertToDeterministicDetectConflict()
		{
			var automata = new NonDeterministicAutomata<int>();
			var q0 = automata.PushEmpty();
			var q1 = automata.PushValue(17);
			var q2 = automata.PushValue(42);

			automata.BranchTo(q0, 'a', 'a', q1);
			automata.BranchTo(q0, 'a', 'a', q2);

			Assert.Throws<InvalidOperationException>(() => automata.ConvertToDeterministic(q0));
		}

		[Test]
		public void EpsilonToNextIsConverted()
		{
			var automata = new NonDeterministicAutomata<int>();
			var q0 = automata.PushEmpty();
			var q1 = automata.PushEmpty();
			var q2 = automata.PushValue(1);

			automata.BranchTo(q0, 'a', 'a', q1);
			automata.EpsilonTo(q0, q1);
			automata.EpsilonTo(q1, q2);

			var (dAutomata, d0) = automata.ConvertToDeterministic(q0);

			Assert.That(dAutomata.States[d0].Branches.Count, Is.EqualTo(1));
			Assert.That(dAutomata.States[d0].Branches[0].Begin, Is.EqualTo('a'));
			Assert.That(dAutomata.States[d0].Branches[0].End, Is.EqualTo('a'));
			Assert.That(dAutomata.States[d0].HasValue, Is.True);
			Assert.That(dAutomata.States[d0].Value, Is.EqualTo(1));

			var d1 = dAutomata.States[d0].Branches[0].Target;

			Assert.That(dAutomata.States[d1].Branches.Count, Is.EqualTo(0));
			Assert.That(dAutomata.States[d1].HasValue, Is.True);
			Assert.That(dAutomata.States[d1].Value, Is.EqualTo(1));
		}

		[Test]
		public void EpsilonToSelfIsIgnored()
		{
			var automata = new NonDeterministicAutomata<int>();
			var q0 = automata.PushEmpty();
			var q1 = automata.PushValue(1);

			automata.BranchTo(q0, 'a', 'a', q1);
			automata.EpsilonTo(q1, q1);

			var (dAutomata, d0) = automata.ConvertToDeterministic(q0);

			Assert.That(dAutomata.States[d0].Branches.Count, Is.EqualTo(1));
			Assert.That(dAutomata.States[d0].Branches[0].Begin, Is.EqualTo('a'));
			Assert.That(dAutomata.States[d0].Branches[0].End, Is.EqualTo('a'));
			Assert.That(dAutomata.States[d0].HasValue, Is.False);

			var d1 = dAutomata.States[d0].Branches[0].Target;

			Assert.That(dAutomata.States[d1].Branches.Count, Is.EqualTo(0));
			Assert.That(dAutomata.States[d1].HasValue, Is.True);
			Assert.That(dAutomata.States[d1].Value, Is.EqualTo(1));
		}
	}
}
