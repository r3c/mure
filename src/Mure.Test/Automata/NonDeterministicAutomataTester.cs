using System;
using System.Reflection;
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

			q0.BranchTo('a', 'a', q1);
			q0.BranchTo('a', 'a', q2);

			Assert.Throws<InvalidOperationException>(() => q0.ToDeterministic());
		}

		[Test]
		public void EpsilonToNextIsConverted()
		{
			var automata = new NonDeterministicAutomata<int>();
			var q0 = automata.PushEmpty();
			var q1 = automata.PushEmpty();
			var q2 = automata.PushValue(1);

			q0.BranchTo('a', 'a', q1);
			q0.EpsilonTo(q1);
			q1.EpsilonTo(q2);

			var deterministic = q0.ToDeterministic();
			var d0 = deterministic.Start;
			var state0 = deterministic.States[d0];

			Assert.That(state0.Branches.Count, Is.EqualTo(1));
			Assert.That(state0.Branches[0].Begin, Is.EqualTo('a'));
			Assert.That(state0.Branches[0].End, Is.EqualTo('a'));
			Assert.That(state0.HasValue, Is.True);
			Assert.That(state0.Value, Is.EqualTo(1));

			Assert.That(deterministic.TryFollow(deterministic.Start, 'a', out var d1), Is.True);

			var state1 = deterministic.States[d1];

			Assert.That(state1.Branches.Count, Is.EqualTo(0));
			Assert.That(state1.HasValue, Is.True);
			Assert.That(state1.Value, Is.EqualTo(1));
		}

		[Test]
		public void EpsilonToSelfIsIgnored()
		{
			var automata = new NonDeterministicAutomata<int>();
			var q0 = automata.PushEmpty();
			var q1 = automata.PushValue(1);

			q0.BranchTo('a', 'a', q1);
			q1.EpsilonTo(q1);

			var deterministic = q0.ToDeterministic();
			var d0 = deterministic.Start;
			var state0 = deterministic.States[d0];

			Assert.That(state0.Branches.Count, Is.EqualTo(1));
			Assert.That(state0.Branches[0].Begin, Is.EqualTo('a'));
			Assert.That(state0.Branches[0].End, Is.EqualTo('a'));
			Assert.That(state0.HasValue, Is.False);

			Assert.That(deterministic.TryFollow(d0, 'a', out var d1), Is.True);

			var state1 = deterministic.States[d1];

			Assert.That(state1.Branches.Count, Is.EqualTo(0));
			Assert.That(state1.HasValue, Is.True);
			Assert.That(state1.Value, Is.EqualTo(1));
		}
	}
}
