using RazorEngine.Compilation;
using RazorEngine.Compilation.ReferenceResolver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MailSender
{
    public class CustomAssembliesReferenceResolver : IReferenceResolver
    {
        public IEnumerable<CompilerReference> GetReferences(TypeContext context = null, IEnumerable<CompilerReference> includeAssemblies = null)
        {
            return (from a in CompilerServicesUtility.GetLoadedAssemblies().Where(delegate (Assembly a)
            {
                if (!a.IsDynamic)
                {
                    try
                    {
                        // Call to .Location property throws an exception on some assemblies, something weird with file://?/C:/...
                        if (File.Exists(a.Location))
                        {
                            return !a.Location.Contains("CompiledRazorTemplates.Dynamic");
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }
                return false;
            })
                    group a by a.GetName().Name into grp
                    select grp.First((Assembly y) => y.GetName().Version == grp.Max((Assembly x) => x.GetName().Version)) into a
                    select CompilerReference.From(a)).Concat(includeAssemblies ?? Enumerable.Empty<CompilerReference>());
        }
    }
}
