using Eva.Commons.Messages;
using Eva.Commons.Security.Certificate;
using Eva.Commons.System;
using Eva.Node.Network;
using Eva.Node.Service;
using Eva.Node.Service.Functions;
using Jint;
using Jint.Native;
using Jint.Native.Function;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;

namespace Eva.Node.Terminal;

public class TerminalSession : IDisposable{
    
    private readonly Engine _engine;
    private Certificate? _currentCert;
    
    public TerminalSession(Certificate cert)
    {
        _engine = new Engine(opts => opts
            .LimitRecursion(16)
            .TimeoutInterval(TimeSpan.FromSeconds(30))
        );

        BuildServices();
    }

    private void BuildServices()
    {
        if(_engine.Global.HasProperty("eva")) throw new Exception("Terminal session is already running. Please restart the session to modify services objects");
        
        var evaObj = new JsObject(_engine);
        _engine.SetValue("eva", evaObj);
        
        foreach (var node in EvaSystem.Singleton<INetworkNodeManager>().Nodes)
        {
            BuildServiceObject(node);
        }
        BuildServiceObject(ServiceLoader.Instance!.Description!.Name, EvaSystem.Singleton<IFunctionRegistry>()!.GetPanel()); //Own service
    }

    private void BuildServiceObject(NodeEntity service)
    {
        if(service.FunctionPanel is null) return;
        BuildServiceObject(service.Name, service.FunctionPanel);
    }
    
    private void BuildServiceObject(string name, FunctionPanel panel, JsObject? parent = null)
    {
        var nodeObj = new JsObject(_engine);

        foreach (var func in panel.Functions)
        {
            var capturedFunc = func;
            var capturedService = name;
            var function = new ClrFunction(_engine, "log", (thisObj, args) =>
            {
                var parameters = new object?[args.Length];
                for (int i = 0; i < args.Length; i++)
                    parameters[i] = TerminalUtil.ConvertFromJavascript(args[i], capturedFunc.Parameters[i].Type.Type);
                
                var response = EvaServices.Call<byte[]>(
                    $"{capturedService}.{capturedFunc.Name}",
                    _currentCert,
                    parameters
                ).GetAwaiter().GetResult();


                return TerminalUtil.ConvertToJavascript(response, capturedFunc.ReturnType.Type);
            });
            nodeObj.FastSetProperty(func.Name, new PropertyDescriptor(function, false, true, false)); 
        }
        
        if(parent is null)
            ((JsObject)_engine.GetValue("eva")).FastSetProperty(name, new PropertyDescriptor(nodeObj, false, true, false));
        else
            parent.FastSetProperty(name.Split(".")[-1], new PropertyDescriptor(nodeObj, false, true, false));
    }
    
    public JsValue Execute(string script, Certificate cert)
    {
        _currentCert = cert;
        return _engine.Evaluate(script);
    }
    
    public void Dispose()
    {
        _engine.Dispose();
    }
}