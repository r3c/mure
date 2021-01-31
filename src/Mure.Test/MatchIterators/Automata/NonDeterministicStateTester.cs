using System;
using Mure.MatchIterators.Automata;
using NUnit.Framework;

namespace Mure.Test.Matchers.Automata
{
	public class NonDeterministicStateTester
	{
		[Test]
		public void ConvertToDeterministicDetectConflict()
		{
			var q0 = new NonDeterministicState<int>();
			var q1 = new NonDeterministicState<int>(17);
			var q2 = new NonDeterministicState<int>(42);

			q0.ConnectTo('a', 'a', q1);
			q0.ConnectTo('a', 'a', q2);

			Assert.Throws<InvalidOperationException>(() => q0.ConvertToDeterministic());
		}

		[Test]
		public void EpsilonToNextIsConverted()
		{
			var q0 = new NonDeterministicState<int>();
			var q1 = new NonDeterministicState<int>();
			var q2 = new NonDeterministicState<int>(1);

			q0.ConnectTo('a', 'a', q1);
			q0.EpsilonTo(q1);
			q1.EpsilonTo(q2);

			var d0 = q0.ConvertToDeterministic();

			Assert.That(d0.Branches.Count, Is.EqualTo(1));
			Assert.That(d0.Branches[0].Begin, Is.EqualTo('a'));
			Assert.That(d0.Branches[0].End, Is.EqualTo('a'));
			Assert.That(d0.HasValue, Is.True);
			Assert.That(d0.Value, Is.EqualTo(1));

			var d1 = d0.Branches[0].Value;

			Assert.That(d1.Branches.Count, Is.EqualTo(0));
			Assert.That(d1.HasValue, Is.True);
			Assert.That(d1.Value, Is.EqualTo(1));
		}

		[Test]
		public void EpsilonToSelfIsIgnored()
		{
			var q0 = new NonDeterministicState<int>();
			var q1 = new NonDeterministicState<int>(1);

			q0.ConnectTo('a', 'a', q1);
			q1.EpsilonTo(q1);

			var d0 = q0.ConvertToDeterministic();

			Assert.That(d0.Branches.Count, Is.EqualTo(1));
			Assert.That(d0.Branches[0].Begin, Is.EqualTo('a'));
			Assert.That(d0.Branches[0].End, Is.EqualTo('a'));
			Assert.That(d0.HasValue, Is.False);

			var d1 = d0.Branches[0].Value;

			Assert.That(d1.Branches.Count, Is.EqualTo(0));
			Assert.That(d1.HasValue, Is.True);
			Assert.That(d1.Value, Is.EqualTo(1));
		}
	}
}
