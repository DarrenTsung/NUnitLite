
namespace NUnitLite {
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Linq;

	public class TestAttribute : Attribute {}

	public static class TestRunner {
		private static readonly Action<string> kDefaultLogCallback = (s) => Console.WriteLine(s);
		public static void RunAllTests(Action<string> logCallback = null) {
			int totalTests = 0, totalPassed = 0;
			logCallback = logCallback ?? kDefaultLogCallback;
			foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())) {
				foreach (var testMethodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Static).Where(m => m.IsDefined(typeof(TestAttribute), inherit: false))) {
					string testResult = null;
					totalTests++;
					try {
						testMethodInfo.Invoke(null, null);
						testResult = string.Format("✓ Pass - {0}", testMethodInfo.Name);
						totalPassed++;
					} catch (TargetInvocationException exception) {
						if (exception.InnerException is TestFailedException) { testResult = string.Format("✖ Fail - {0}\n    Reason: {1}", testMethodInfo.Name, exception.InnerException.Message); }
						else { testResult = string.Format("✖ Fail - {0}\n    Reason: Threw unhandled exception - {1}: {2}", testMethodInfo.Name, exception.InnerException.GetType().Name, exception.InnerException.Message); }
					}
					logCallback.Invoke(testResult);
				}
			}
			int totalFailed = totalTests - totalPassed;
			logCallback.Invoke(string.Format("Total tests: {0}. Passed: {1}. Failed: {2}.", totalTests, totalPassed, totalFailed));
			for (int i = 0; i < totalFailed; i++) { logCallback.Invoke(""); } // NOTE (darren): to fix bug in coderpad.io where console is cut off
		}
	}

	public class TestFailedException : Exception {
		public TestFailedException(string error) : base(error) {}
	}

	public static class Assert {
		public static void That(object output, Constraint constraint) {
			constraint.CheckSatisfiedBy(output);
		}

		public static void Throws<T>(Action methodWrapper) {
			try { methodWrapper.Invoke(); }
			catch (Exception e) { if (e is T) { return; } else { throw e; } }
			throw new TestFailedException(string.Format("Failed to throw exception of type {0}!", typeof(T).Name));
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
			Type expectedType = expected_.GetType(), outputType = output.GetType();
			if (outputType != expectedType) { throw new TestFailedException(string.Format("Output is different type, expected type is {0}, output type is {1}!", expectedType.Name, outputType.Name)); }
			if (expectedType.IsArray) { IList outputList = (IList)output, expectedList = (IList)expected_;
				if (!ListsMatch(expectedList, outputList)) { throw new TestFailedException(string.Format("Output list does match expected list, expected is [{0}], output is [{1}]!", StringifyList(expectedList), StringifyList(outputList))); }
			} else { if (!output.Equals(expected_)) { throw new TestFailedException(string.Format("Output is incorrect, expected is {0}, output is {1}!", expected_, output)); } }
		}
		private static bool ListsMatch(IList a, IList b) {
			if (a.Count != b.Count) { return false; }
			for (int i = 0; i < a.Count; i++) { if (!a[i].Equals(b[i])) { return false; } }
			return true;
		}
		private static string StringifyList(IList list) {
			string[] elements = new string[list.Count];
			for (int i = 0; i < list.Count; i++) { elements[i] = list[i].ToString(); }
			return string.Join(", ", elements);
		}
		private object expected_;
	}
}
