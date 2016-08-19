#if !EXCLUDE_CODEGEN
#pragma warning disable 162
#pragma warning disable 219
#pragma warning disable 414
#pragma warning disable 649
#pragma warning disable 693
#pragma warning disable 1591
#pragma warning disable 1998
[assembly: global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.3.0")]
[assembly: global::Orleans.CodeGeneration.OrleansCodeGenerationTargetAttribute("GrainInterfaces, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
namespace GrainInterfaces
{
    using global::Orleans.Async;
    using global::Orleans;

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.3.0"), global::System.SerializableAttribute, global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute, global::Orleans.CodeGeneration.GrainReferenceAttribute(typeof (global::GrainInterfaces.IHelloGrain))]
    internal class OrleansCodeGenHelloGrainReference : global::Orleans.Runtime.GrainReference, global::GrainInterfaces.IHelloGrain
    {
        protected @OrleansCodeGenHelloGrainReference(global::Orleans.Runtime.GrainReference @other): base (@other)
        {
        }

        protected @OrleansCodeGenHelloGrainReference(global::System.Runtime.Serialization.SerializationInfo @info, global::System.Runtime.Serialization.StreamingContext @context): base (@info, @context)
        {
        }

        protected override global::System.Int32 InterfaceId
        {
            get
            {
                return 1313245371;
            }
        }

        public override global::System.String InterfaceName
        {
            get
            {
                return "global::GrainInterfaces.IHelloGrain";
            }
        }

        public override global::System.Boolean @IsCompatible(global::System.Int32 @interfaceId)
        {
            return @interfaceId == 1313245371;
        }

        protected override global::System.String @GetMethodName(global::System.Int32 @interfaceId, global::System.Int32 @methodId)
        {
            switch (@interfaceId)
            {
                case 1313245371:
                    switch (@methodId)
                    {
                        case -1732333552:
                            return "SayHello";
                        default:
                            throw new global::System.NotImplementedException("interfaceId=" + 1313245371 + ",methodId=" + @methodId);
                    }

                default:
                    throw new global::System.NotImplementedException("interfaceId=" + @interfaceId);
            }
        }

        public global::System.Threading.Tasks.Task<global::System.String> @SayHello()
        {
            return base.@InvokeMethodAsync<global::System.String>(-1732333552, null);
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.3.0"), global::Orleans.CodeGeneration.MethodInvokerAttribute("global::GrainInterfaces.IHelloGrain", 1313245371, typeof (global::GrainInterfaces.IHelloGrain)), global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    internal class OrleansCodeGenHelloGrainMethodInvoker : global::Orleans.CodeGeneration.IGrainMethodInvoker
    {
        public global::System.Threading.Tasks.Task<global::System.Object> @Invoke(global::Orleans.Runtime.IAddressable @grain, global::Orleans.CodeGeneration.InvokeMethodRequest @request)
        {
            global::System.Int32 interfaceId = @request.@InterfaceId;
            global::System.Int32 methodId = @request.@MethodId;
            global::System.Object[] arguments = @request.@Arguments;
            try
            {
                if (@grain == null)
                    throw new global::System.ArgumentNullException("grain");
                switch (interfaceId)
                {
                    case 1313245371:
                        switch (methodId)
                        {
                            case -1732333552:
                                return ((global::GrainInterfaces.IHelloGrain)@grain).@SayHello().@Box();
                            default:
                                throw new global::System.NotImplementedException("interfaceId=" + 1313245371 + ",methodId=" + methodId);
                        }

                    default:
                        throw new global::System.NotImplementedException("interfaceId=" + interfaceId);
                }
            }
            catch (global::System.Exception exception)
            {
                return global::Orleans.Async.TaskUtility.@Faulted(exception);
            }
        }

        public global::System.Int32 InterfaceId
        {
            get
            {
                return 1313245371;
            }
        }
    }
}
#pragma warning restore 162
#pragma warning restore 219
#pragma warning restore 414
#pragma warning restore 649
#pragma warning restore 693
#pragma warning restore 1591
#pragma warning restore 1998
#endif
