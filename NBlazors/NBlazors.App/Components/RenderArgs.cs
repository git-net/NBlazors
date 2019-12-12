namespace NBlazors.App.Components
{
    using System;
    public struct RenderArgs<TTag,TModel>   
    {
        public TTag Tag;
        public TModel Model;
        public object Arg;
        Func<TModel, object, object> _valueGetter;
        public object Value => _valueGetter?.Invoke(Model, Arg);
        public Func<TModel, object, object> GetGetter(Func<TModel, object, object> func) => func;
        public RenderArgs(TTag tag, TModel model, object arg  , Func<TModel, object, object> valueGetter=null) {
            this.Tag = tag;
            this.Model = model;
            this.Arg = arg;
            _valueGetter = valueGetter;
        }
    }
}
