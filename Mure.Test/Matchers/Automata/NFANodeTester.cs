using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mure.Matchers.Automata;
using NUnit.Framework;

namespace Mure.Test.Matchers.Automata
{
	public class NFAStateTester
	{
		[Test]
		public void ConvertToDFADetectConflict()
		{
			var q0 = new NFAState<int>();
			var q1 = new NFAState<int>(17);
			var q2 = new NFAState<int>(42);

			q0.ConnectTo('a', 'a', q1);
			q0.ConnectTo('a', 'a', q2);

			Assert.Throws<InvalidOperationException>(() => q0.ConvertToDFA());
		}

		[Test]
		public void EpsilonToNextIsConverted()
		{
			var q0 = new NFAState<int>();
			var q1 = new NFAState<int>();
			var q2 = new NFAState<int>(1);

			q0.ConnectTo('a', 'a', q1);
			q0.EpsilonTo(q1);
			q1.EpsilonTo(q2);

			var d0 = q0.ConvertToDFA();

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
			var q0 = new NFAState<int>();
			var q1 = new NFAState<int>(1);

			q0.ConnectTo('a', 'a', q1);
			q1.EpsilonTo(q1);

			var d0 = q0.ConvertToDFA();

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
