﻿

using System.Diagnostics;
using JetBrains.Annotations;

namespace Reusable.OmniLog.SemanticExtensions
{
    #region Abstractions

    /*
     
     Abstraction
        > Layer (Business | Infrastructure | ...) 
            > Category (Data | Action) 
                > Dump (Object | Property | ...)
     
    logger.Log(Abstraction.Layer.Business().Data().Object(new { customer }));
    logger.Log(Abstraction.Layer.Infrastructure().Data().Variable(new { customer }));
    logger.Log(Abstraction.Layer.Infrastructure().Action().Failed(nameof(Main), ex));

     */

    // Base interface for the first tier "layer"
    public interface IAbstraction { }

    public interface IAbstractionLayer
    {
        void Deconstruct(out string name, out LogLevel logLevel);
    }

    // Hides the two properties from the extensions.
    public interface IAbstractionLayerCategory
    {
        IAbstractionLayer Layer { get; }

        string Name { get; }
    }

    // Allows to write extensions against the Data catagory.
    public interface IAbstractionLayerData : IAbstractionLayerCategory { }

    // Allows to write extensions against the Action catagory.
    public interface IAbstractionLayerAction : IAbstractionLayerCategory { }

    public interface IAbstractionLayerEvent : IAbstractionLayerCategory { }

    // The result of building the abstraction.
    public interface IAbstractionContext
    {
        LogLevel LogLevel { get; }

        string LayerName { get; }

        string CategoryName { get; }

        object Dump { get; }
    }

    #endregion

    #region Implementations

    public abstract class Abstraction
    {
        public static IAbstraction Layer => default;
    }

    public class AbstractionLayer : IAbstractionLayer
    {
        private readonly string _name;

        private readonly LogLevel _logLevel;

        public AbstractionLayer(string name, LogLevel logLevel)
        {
            _name = name;
            _logLevel = logLevel;
        }

        public void Deconstruct(out string name, out LogLevel logLevel)
        {
            name = _name;
            logLevel = _logLevel;
        }
    }

    public abstract class AbstractionLayerCategory : IAbstractionLayerCategory
    {
        protected AbstractionLayerCategory(IAbstractionLayer layer, string name)
        {
            Layer = layer;
            Name = name;
        }

        public IAbstractionLayer Layer { get; }

        public string Name { get; }
    }

    public class AbstractionLayerData : AbstractionLayerCategory, IAbstractionLayerData
    {
        public AbstractionLayerData(IAbstractionLayer layer, string name) : base(layer, name) { }
    }

    public class AbstractionLayerAction : AbstractionLayerCategory, IAbstractionLayerAction
    {
        public AbstractionLayerAction(IAbstractionLayer layer, string name) : base(layer, name) { }
    }

    public class AbstractionLayerEvent : AbstractionLayerCategory, IAbstractionLayerEvent
    {
        public AbstractionLayerEvent(IAbstractionLayer layer, string name) : base(layer, name) { }
    }

    public class AbstractionContext : IAbstractionContext
    {
        public AbstractionContext(IAbstractionLayerCategory layerCategory, object dump)
        {
            (LayerName, LogLevel) = layerCategory.Layer;
            CategoryName = layerCategory.Name;
            Dump = dump;
        }

        public LogLevel LogLevel { get; }

        public string LayerName { get; }

        public string CategoryName { get; }

        public object Dump { get; }
    }

    #endregion

    #region Extensions


    public static class AbstractionExtensions
    {
        #region Information

        public static IAbstractionLayer Business(this IAbstraction abstraction)
        {
            return new AbstractionLayer(nameof(Business), LogLevel.Information);
        }

        #endregion

        #region Debug

        public static IAbstractionLayer Infrastructure(this IAbstraction abstraction)
        {
            return new AbstractionLayer(nameof(Infrastructure), LogLevel.Debug);
        }

        public static IAbstractionLayer Logging(this IAbstraction abstraction)
        {
            return new AbstractionLayer(nameof(Logging), LogLevel.Debug);
        }

        #endregion

        #region Trace

        public static IAbstractionLayer Presentation(this IAbstraction abstraction)
        {
            return new AbstractionLayer(nameof(Presentation), LogLevel.Trace);
        }

        public static IAbstractionLayer IO(this IAbstraction abstraction)
        {
            return new AbstractionLayer(nameof(IO), LogLevel.Trace);
        }

        public static IAbstractionLayer Database(this IAbstraction abstraction)
        {
            return new AbstractionLayer(nameof(Database), LogLevel.Trace);
        }

        public static IAbstractionLayer Network(this IAbstraction abstraction)
        {
            return new AbstractionLayer(nameof(Network), LogLevel.Trace);
        }

        #endregion

        //public static IAbstractionLayer External(this IAbstraction abstraction, LogLevel logLevel = null)
        //{
        //    return new AbstractionLayer(nameof(External), logLevel ?? LogLevel.Trace);
        //}
    }

    public static class AbstractionLayerExtensions
    {
        public static IAbstractionLayerData Data(this IAbstractionLayer layer)
        {
            return new AbstractionLayerData(layer, nameof(Data));
        }

        public static IAbstractionLayerAction Action(this IAbstractionLayer layer)
        {
            return new AbstractionLayerAction(layer, nameof(Action));
        }

        //public static IAbstractionLayerEvent Event(this IAbstractionLayer layer)
        //{
        //    return new AbstractionLayerEvent(layer, nameof(Event));
        //}
    }

    // These extensions need to recreate the AbstractionLayerData with a new name 
    // because the identifier is the name of the extension and not Data.

    public static class AbstractionLayerDataExtensions
    {
        /// <summary>
        /// Logs object dumps. The dump object must be an anonymous type with at leas one property: new { foo[, bar] }
        /// </summary>
        public static IAbstractionContext Object(this IAbstractionLayerData data, object dump)
        {
            return new AbstractionContext(new AbstractionLayerData(data.Layer, nameof(Object)), dump);
        }

        /// <summary>
        /// Logs field dumps. The dump object must be an anonymous type with at leas one property: new { foo[, bar] }
        /// </summary>
        public static IAbstractionContext Field(this IAbstractionLayerData data, object dump)
        {
            return new AbstractionContext(new AbstractionLayerData(data.Layer, nameof(Field)), dump);
        }

        /// <summary>
        /// Logs variable dumps. The dump object must be an anonymous type with at leas one property: new { foo[, bar] }
        /// </summary>
        public static IAbstractionContext Variable(this IAbstractionLayerData data, object dump)
        {
            return new AbstractionContext(new AbstractionLayerData(data.Layer, nameof(Variable)), dump);
        }


        /// <summary>
        /// Logs argument dumps. The dump object must be an anonymous type with at leas one property: new { foo[, bar] }
        /// </summary>
        public static IAbstractionContext Argument(this IAbstractionLayerData data, object dump)
        {
            return new AbstractionContext(new AbstractionLayerData(data.Layer, nameof(Argument)), dump);
        }

        /// <summary>
        /// Logs property dumps. The dump object must be an anonymous type with at leas one property: new { foo[, bar] }
        /// </summary>
        public static IAbstractionContext Property(this IAbstractionLayerData data, object dump)
        {
            return new AbstractionContext(new AbstractionLayerData(data.Layer, nameof(Property)), dump);
        }

        /// <summary>
        /// Logs property dumps. The dump object must be an anonymous type with at leas one property: new { foo[, bar] }
        /// </summary>
        public static IAbstractionContext Metric(this IAbstractionLayerData data, object dump)
        {
            return new AbstractionContext(new AbstractionLayerData(data.Layer, nameof(Metric)), dump);
        }
    }

    // The property name of each dump maps to the Identifier column.

    public static class AbstractionLayerActionExtensions
    {
        public static IAbstractionContext Started(this IAbstractionLayerAction action, string methodName)
        {
            return new AbstractionContext(action, new { Started = methodName });
        }

        public static IAbstractionContext Finished(this IAbstractionLayerAction action, string methodName)
        {
            return new AbstractionContext(action, new { Finished = methodName });
        }

        public static IAbstractionContext Cancelled(this IAbstractionLayerAction action, string methodName)
        {
            return new AbstractionContext(action, new { Canceled = methodName });
        }

        public static IAbstractionContext Failed(this IAbstractionLayerAction action, string methodName)
        {
            return new AbstractionContext(action, new { Failed = methodName });
        }
    }

    //public static class AbstractionLayerEventExtensions
    //{
    //    public static IAbstractionContext On(this IAbstractionLayerEvent context, string eventName)
    //    {
    //        return new AbstractionContext(context, new { On = eventName });
    //    }
    //}

    #endregion
}