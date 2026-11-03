using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace hp55games.MapGame.RulesHarness
{
    /// <summary>
    /// Runner minimale per i test NUnit delle regole: [Test], [TestCase], [SetUp], [TearDown].
    /// Argomento opzionale: filtro sul nome completo del test. Exit code 0 = tutto verde.
    /// </summary>
    internal static class Runner
    {
        private static int Main(string[] args)
        {
            string filter = args.Length > 0 ? args[0] : null;
            int passed = 0, failed = 0;
            var fixtures = typeof(Runner).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.GetMethods().Any(IsTest))
                .OrderBy(t => t.FullName);

            foreach (var type in fixtures)
            {
                var setUp = type.GetMethods().FirstOrDefault(m => m.IsDefined(typeof(SetUpAttribute), true));
                var tearDown = type.GetMethods().FirstOrDefault(m => m.IsDefined(typeof(TearDownAttribute), true));

                foreach (var method in type.GetMethods().Where(IsTest).OrderBy(m => m.Name))
                {
                    var cases = method.GetCustomAttributes<TestCaseAttribute>(true).Select(c => c.Arguments).ToList();
                    if (cases.Count == 0) cases.Add(Array.Empty<object>());

                    foreach (var arguments in cases)
                    {
                        string name = type.FullName + "." + method.Name +
                                      (arguments.Length > 0 ? "(" + string.Join(",", arguments) + ")" : "");
                        if (filter != null && name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;

                        object instance = Activator.CreateInstance(type);
                        try
                        {
                            setUp?.Invoke(instance, null);
                            try { method.Invoke(instance, arguments); }
                            finally { tearDown?.Invoke(instance, null); }
                            passed++;
                        }
                        catch (TargetInvocationException e)
                        {
                            var inner = e.InnerException;
                            if (inner is SuccessException) { passed++; continue; }
                            failed++;
                            Console.WriteLine("FAIL " + name);
                            Console.WriteLine("     " + (inner?.Message ?? e.Message).Trim().Replace("\n", "\n     "));
                        }
                    }
                }
            }

            Console.WriteLine($"Passati: {passed}  Falliti: {failed}");
            return failed == 0 ? 0 : 1;
        }

        private static bool IsTest(MethodInfo m) =>
            m.IsDefined(typeof(TestAttribute), true) || m.IsDefined(typeof(TestCaseAttribute), true);
    }
}
