using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Cdn.Studio
{
	public class DynamicIntegrator
	{
		private GLib.GType d_gtype;
		private Type d_dynamicType;

		public DynamicIntegrator(string filename)
		{
			AssemblyName name = new AssemblyName("DynamicIntegratorAssembly" +
			                                      Guid.NewGuid().ToString("N"));

			// Assembly builder
			AssemblyBuilder ab =
				AppDomain.CurrentDomain.DefineDynamicAssembly(name,
			                                                  AssemblyBuilderAccess.Run);

			// Module builder
			ModuleBuilder mb = ab.DefineDynamicModule("DynamicIntegrator");

			// Type builder
			TypeBuilder tb = mb.DefineType("DynamicIntegrator" +
			                               Guid.NewGuid().ToString("N"));

			// Method builder
			MethodBuilder meb;

			d_gtype = GLib.GType.Invalid;

			// Define dynamic PInvoke method
			meb = tb.DefinePInvokeMethod("cdn_register_integrator",
			                             filename,
			                             MethodAttributes.Public |
			                             MethodAttributes.Static |
			                             MethodAttributes.PinvokeImpl,
			                             CallingConventions.Standard,
			                             typeof(GLib.GType),
			                             null,
			                             CallingConvention.StdCall,
			                             CharSet.Auto);

			// Implementation flags for preserving signature
			meb.SetImplementationFlags(meb.GetMethodImplementationFlags() |
			                           MethodImplAttributes.PreserveSig);

			// Create the dynamic type
			try
			{
				d_dynamicType = tb.CreateType();
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Could not create dynamic integrator type proxy: {0}", e.Message);
				d_dynamicType = null;
			}
		}
		
		public GLib.GType Register()
		{
			if (d_dynamicType == null)
			{
				return GLib.GType.Invalid;
			}

			if (d_gtype == GLib.GType.Invalid)
			{
				try
				{
					d_gtype = (GLib.GType)d_dynamicType.InvokeMember("cdn_register_integrator",
					                                                 BindingFlags.InvokeMethod,
					                                                 null,
					                                                 Activator.CreateInstance(d_dynamicType), null);
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("Could not invoke integrator register function: {0}", e.Message);
					d_gtype = GLib.GType.Invalid;
					d_dynamicType = null;
				}
			}
			
			return d_gtype;
		}
	}
}
