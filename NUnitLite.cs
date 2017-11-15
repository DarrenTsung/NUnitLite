
namespace NUnitLite {
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Linq;

	public class TestAttribute : Attribute {}

	public static class TestRunner {
		public static void RunAllTests() {
			foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())) {
				foreach (var testMethodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Static).Where(m => m.IsDefined(typeof(TestAttribute), inherit: false))) {
					try {
						testMethodInfo.Invoke(null, null);
						Console.WriteLine(string.Format("✓ Pass - {0}", testMethodInfo.Name));
					} catch (TargetInvocationException exception) {
						if (exception.InnerException is TestFailedException) { Console.WriteLine(string.Format("✖ Fail - {0}\n    Reason: {1}", testMethodInfo.Name, exception.InnerException.Message)); }
						else { Console.WriteLine(string.Format("✖ Fail - {0}\n    Reason: Threw unhandled exception - {1}: {2}", testMethodInfo.Name, exception.InnerException.GetType().Name, exception.InnerException.Message)); }
					}
				}
			}
		}
	}

	public class TestFailedException : Exception {
		public TestFailedException(string error) : base(error) {}
	}

	public static class Assert {
		public static void That(object output, Constraint constraint) {
			constraint.CheckSatisfiedBy(output);
		}
	}

	public static class Is {
		public static Constraint EqualTo(object expected) { return new EqualityConstraint(expected); }
	}

	public abstract class Constraint {
		public abstract void CheckSatisfiedBy(object output);
	}

	public class EqualityConstraint : Constraint {
		public EqualityConstraint(object expected) { expected_ = expected; }
		public override void CheckSatisfiedBy(object output) {
			if (output == null && expected_ != null) { throw new TestFailedException(string.Format("Output is null, expected is {0}!", expected_)); }
			if (output == null && expected_ == null) { return; } // successful case
			if (output.GetType() != expected_.GetType()) { throw new TestFailedException(string.Format("Output is different type, expected type is {0}, output type is {1}!", expected_.GetType().Name, output.GetType().Name)); }
			if (!output.Equals(expected_)) { throw new TestFailedException(string.Format("Output is incorrect, expected is {0}, output is {1}!", expected_, output)); }
		}

		private object expected_;
	}
}

namespace InterviewQuestions.AppleStocks {
	using NUnitLite;
	using UnityEngine;
	using UnityEditor;

	[ExecuteInEditMode]
	[InitializeOnLoad]
	public static class AppleStocksTests {
		static AppleStocksTests() {
			TestRunner.RunAllTests();
		}

		[Test]
		public static void GetMaxProfit_BasicExample_ReturnsExpected() {
			Assert.That(AppleStocks.GetMaxProfit(new int[] { 10, 7, 5, 8, 11, 9 }), Is.EqualTo(6));
		}
	}
}
