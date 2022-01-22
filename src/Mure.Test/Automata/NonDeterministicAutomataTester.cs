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
			var q0 = NonDeterministicNode<int>.Create();
			var q1 = q0.PushValue(17);
			var q2 = q0.PushValue(42);

			q0.BranchTo('a', 'a', q1);
			q0.BranchTo('a', 'a', q2);

			Assert.Throws<InvalidOperationException>(() => q0.ToDeterministicNode());
		}

		[Test]
		public void EpsilonToNextIsConverted()
		{
			var q0 = NonDeterministicNode<int>.Create();
			var q1 = q0.PushEmpty();
			var q2 = q0.PushValue(1);

			q0.BranchTo('a', 'a', q1);
			q0.EpsilonTo(q1);
			q1.EpsilonTo(q2);

			var (dAutomata, d0) = q0.ToDeterministicNode();

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
			var q0 = NonDeterministicNode<int>.Create();
			var q1 = q0.PushValue(1);

			q0.BranchTo('a', 'a', q1);
			q1.EpsilonTo(q1);

			var (dAutomata, d0) = q0.ToDeterministicNode();

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
