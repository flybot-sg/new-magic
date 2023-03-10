/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

/**
 *   Author: David Miller
 **/

using System;
using System.Runtime.Serialization;

namespace clojure.lang
{

    /// <summary>
    /// Represents a namespace for holding symbol-&gt;reference mappings.
    /// </summary>
    /// <remarks>
    /// <para>Symbol to reference mappings come in several flavors:
    /// <list>
    /// <item><b>Simple:</b> <see cref="Symbol">Symbol</see> to a <see cref="Var">Var</see> in the namespace.</item>
    /// <item><b>Use/refer:</b> <see cref="Symbol">Symbol</see> to a <see cref="Var">Var</see> that is homed in another namespace.</item>
    /// <item>Import:</item> <see cref="Symbol">Symbol</see> to a Type
    /// </list>
    /// </para>
    /// <para>One namespace can also refer to another namespace by an alias.</para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2229:ImplementSerializationConstructors",Justification="Not needed")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Namespace")]
    [Serializable]
    public class Namespace : AReference, ISerializable
    {
        #region Data

        /// <summary>
        /// The namespace's name.
        /// </summary>
        private readonly Symbol _name;

        /// <summary>
        /// The namespace's name.
        /// </summary>
        public  Symbol Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Maps <see cref="Symbol">Symbol</see>s to their values (Types, <see cref="Var">Var</see>s, or arbitrary).
        /// </summary>
        [NonSerialized]
        private readonly AtomicReference<IPersistentMap> _mappings = new AtomicReference<IPersistentMap>();

        /// <summary>
        /// Maps <see cref="Symbol">Symbol</see>s to other namespaces (aliases).
        /// </summary>
        [NonSerialized]
        private readonly AtomicReference<IPersistentMap> _aliases = new AtomicReference<IPersistentMap>();


        // Why not use one of the IPersistentMap implementations?
        /// <summary>
        /// All namespaces, keyed by <see cref="Symbol">Symbol</see>.
        /// </summary>
        private static JavaConcurrentDictionary<Symbol, Namespace> _namespaces
            = new JavaConcurrentDictionary<Symbol, Namespace>();

   
        /// <summary>
        /// Get the variable-to-value map.
        /// </summary>
        private IPersistentMap Mappings
        {
            get { return _mappings.Get(); }
            //set { mappings = value; }
        }
        /// <summary>
        /// Get the variable-to-namespace alias map.
        /// </summary>
        private IPersistentMap Aliases
        {
            get { return _aliases.Get(); }
        }

        /// <summary>
        /// Get all namespaces.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "all")]
        public static ISeq all
        {
            get
            { return RT.seq(_namespaces.Values); }
        }

        #endregion

        #region C-tors & factory methods

        /// <summary>
        /// Find or create a namespace named by the symbol.
        /// </summary>
        /// <param name="name">The symbol naming the namespace.</param>
        /// <returns>An existing or new namespace</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "find")]
        public static Namespace findOrCreate(Symbol name)
        {
            Namespace ns = _namespaces.Get(name);
            if (ns != null)
                return ns;
            Namespace newns = new Namespace(name);
            ns = _namespaces.PutIfAbsent(name, newns);
            return ns == null ? newns : ns;
        }

        /// <summary>
        /// Remove a namespace (by name).
        /// </summary>
        /// <param name="name">The (Symbol) name of the namespace to remove.</param>
        /// <returns>The namespace that was removed.</returns>
        /// <remarks>Trying to remove the clomure namespace throws an exception.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "remove")]
        public static Namespace remove(Symbol name)
        {
            if (name.Equals(RT.ClojureNamespace.Name))
                throw new ArgumentException("Cannot remove clojure namespace");
            return _namespaces.Remove(name);
        }

        /// <summary>
        /// Find the namespace with a given name.
        /// </summary>
        /// <param name="name">The name of the namespace to find.</param>
        /// <returns>The namespace with the given name, or <value>null</value> if no such namespace exists.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "find")]
        public static Namespace find(Symbol name)
        {
            return _namespaces.Get(name);
        }
        

        /// <summary>
        /// Construct a namespace with a given name.
        /// </summary>
        /// <param name="name">The name.</param>
        Namespace(Symbol name)
            : base(name.meta())
        {
            _name = name;
            _mappings.Set(RT.DEFAULT_IMPORTS);
            _aliases.Set(RT.map());
        }

         #endregion

        #region Object overrides

        /// <summary>
        /// Returns a string representing the namespace.
        /// </summary>
        /// <returns>A string representing the namespace.</returns>
        public override string ToString()
        {
            return _name.ToString();
        }

        #endregion

        #region Interning 


        public static bool AreDifferentInstancesOfSameClassName(Type t1, Type t2)
        {
            return (t1 != t2) && (t1.FullName.Equals(t2.FullName));
        }

        Type ReferenceClass(Symbol sym, Type val)
        {
            if (sym.Namespace != null)
            {
                throw new ArgumentException("Can't intern namespace-qualified symbol");
            }
            IPersistentMap map = getMappings();
            Type c = (Type)map.valAt(sym);
            while ((c == null) || (AreDifferentInstancesOfSameClassName(c, val)))
            {
                IPersistentMap newMap = map.assoc(sym, val);
                _mappings.CompareAndSet(map, newMap);
                map = getMappings();
                c = (Type)map.valAt(sym);
            }
            if (c == val)
                return c;

            throw new InvalidOperationException(sym + " already refers to: " + c + " in namespace: " + Name);
        }


        /// <summary>
        /// Intern a <see cref="Symbol">Symbol</see> in the namespace, with a (new) <see cref="Var">Var</see> as its value.
        /// </summary>
        /// <param name="sym">The symbol to intern.</param>
        /// <returns>The <see cref="Var">Var</see> associated with the symbol.</returns>
        /// <remarks>
        /// <para>It is an error to intern a symbol with a namespace.</para>
        /// <para>This has to deal with other threads also interning.</para>
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "intern")]
        public Var intern(Symbol sym)
        {
            if (sym.Namespace != null)
                throw new ArgumentException("Can't intern a namespace-qualified symbol");

            IPersistentMap map = Mappings;
            object o;
            Var v = null;
            // race condition
            while ((o = map.valAt(sym)) == null)
            {
                if (v == null)
                    v = new Var(this, sym);
                IPersistentMap newMap = map.assoc(sym, v);
                _mappings.CompareAndSet(map, newMap);
                map = Mappings;
            }

            Var ovar = o as Var;

            if (ovar != null && ovar.Namespace == this)
                return ovar;

            if (v == null)
                v = new Var(this, sym);

            WarnOrFailOnReplace(sym, o, v);

            while (!_mappings.CompareAndSet(map, map.assoc(sym, v)))
                map = Mappings;

            return v;
        }

        private void WarnOrFailOnReplace(Symbol sym, object o, object v)
        {
            Var ovar = o as Var;

            if (ovar != null)
            {
                Namespace ns = ovar.Namespace;
                Var vv = v as Var;
                if (ns == this || (vv != null && vv.ns == RT.ClojureNamespace))
                    return;
                if (ns != RT.ClojureNamespace)
                    throw new InvalidOperationException(sym + " already refers to: " + o + " in namespace: " + _name);
            }
            RT.errPrintWriter().WriteLine("WARNING: {0} already refers to: {1} in namespace: {2}, being replaced by: {3}",
                sym, o, _name, v);
        }

        /// <summary>
        /// Intern a symbol with a specified value.
        /// </summary>
        /// <param name="sym">The symbol to intern.</param>
        /// <param name="val">The value to associate with the symbol.</param>
        /// <returns>The value that is associated. (only guaranteed == to the value given).</returns>
        object reference(Symbol sym, object val)
        {
            if ( sym.Namespace != null )
                throw new ArgumentException("Can't intern a namespace-qualified symbol");

            IPersistentMap map = Mappings;
            object o;

            // race condition
            while ((o = map.valAt(sym)) == null)
            {
                IPersistentMap newMap = map.assoc(sym, val);
                _mappings.CompareAndSet(map, newMap);
                map = Mappings;
            }

            if ( o == val )
                return o;

            WarnOrFailOnReplace(sym, o, val);

            while (!_mappings.CompareAndSet(map, map.assoc(sym, val)))
                map = Mappings;

            return val;
        }

        /// <summary>
        /// Remove a symbol mapping from the namespace.
        /// </summary>
        /// <param name="sym">The symbol to remove.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "unmap")]
        public void unmap(Symbol sym)
        {
            if (sym.Namespace != null)
                throw new ArgumentException("Can't unintern a namespace-qualified symbol");

            IPersistentMap map = Mappings;
            while (map.containsKey(sym))
            {
                IPersistentMap newMap = map.without(sym);
                _mappings.CompareAndSet(map, newMap);
                map = Mappings;
            }
        }
        
        /// <summary>
        /// Map a symbol to a Type (import).
        /// </summary>
        /// <param name="sym">The symbol to associate with a Type.</param>
        /// <param name="t">The type to associate with the symbol.</param>
        /// <returns>The Type.</returns>
        /// <remarks>Named importClass instead of ImportType for core.clj compatibility.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "import")]
        public Type importClass(Symbol sym, Type t)
        {
            return ReferenceClass(sym, t);
        }


        /// <summary>
        /// Map a symbol to a Type (import) using the type name for the symbol name.
        /// </summary>
        /// <param name="t">The type to associate with the symbol</param>
        /// <returns>The Type.</returns>
        /// <remarks>Named importClass instead of ImportType for core.clj compatibility.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "import")]
        public Type importClass(Type t)
        {
            string n = Util.NameForType(t);   
            return importClass(Symbol.intern(n), t);
        }

        /// <summary>
        /// Add a <see cref="Symbol">Symbol</see> to <see cref="Var">Var</see> reference.
        /// </summary>
        /// <param name="sym"></param>
        /// <param name="var"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "refer")]
        public Var refer(Symbol sym, Var var)
        {
            return (Var)reference(sym, var);
        }

        #endregion

        #region Mappings

        /// <summary>
        /// Get the value mapped to a symbol.
        /// </summary>
        /// <param name="name">The symbol to look up.</param>
        /// <returns>The mapped value.</returns>
        public object GetMapping(Symbol name)
        {
            return Mappings.valAt(name);
        }

        /// <summary>
        /// Find the <see cref="Var">Var</see> mapped to a <see cref="Symbol">Symbol</see>.
        /// </summary>
        /// <param name="sym">The symbol to look up.</param>
        /// <returns>The mapped var.</returns>
        public Var FindInternedVar(Symbol sym)
        {
            Var v = Mappings.valAt(sym) as Var;
            return (v != null && v.Namespace == this) ? v : null;
        }

        #endregion

        #region Aliases

        /// <summary>
        /// Find the <see cref="Namespace">Namespace</see> aliased by a <see cref="Symbol">Symbol</see>.
        /// </summary>
        /// <param name="alias">The symbol alias.</param>
        /// <returns>The aliased namespace</returns>
        public Namespace LookupAlias(Symbol alias)
        {
            return (Namespace)Aliases.valAt(alias);
        }

        /// <summary>
        /// Add an alias for a namespace.
        /// </summary>
        /// <param name="alias">The alias for the namespace.</param>
        /// <param name="ns">The namespace being aliased.</param>
        /// <remarks>Lowercase name for core.clj compatibility</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "add")]
        public void addAlias(Symbol alias, Namespace ns)
        {
            if (alias == null)
                throw new ArgumentNullException("alias","Expecting Symbol + Namespace");
            if ( ns == null )
                throw new ArgumentNullException("ns", "Expecting Symbol + Namespace");

            IPersistentMap map = Aliases;

            // race condition
            while (!map.containsKey(alias))
            {
                IPersistentMap newMap = map.assoc(alias, ns);
                _aliases.CompareAndSet(map, newMap);
                map = Aliases;
            }
            // you can rebind an alias, but only to the initially-aliased namespace
            if (!map.valAt(alias).Equals(ns))
                throw new InvalidOperationException(String.Format("Alias {0} already exists in namespace {1}, aliasing {2}",
                    alias, _name, map.valAt(alias)));
        }

        /// <summary>
        /// Remove an alias.
        /// </summary>
        /// <param name="alias">The alias name</param>
        /// <remarks>Lowercase name for core.clj compatibility</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "remove")]
        public void removeAlias(Symbol alias)
        {
            IPersistentMap map = Aliases;
            while (map.containsKey(alias))
            {
                IPersistentMap newMap = map.without(alias);
                _aliases.CompareAndSet(map, newMap);
                map = Aliases;
            }
        }


        #endregion

        #region core.clj compatibility

        /// <summary>
        /// Get the namespace name.
        /// </summary>
        /// <returns>The <see cref="Symbol">Symbol</see> naming the namespace.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "get")]
        public Symbol getName()
        {
            return Name;
        }

        /// <summary>
        /// Get the mappings of the namespace.
        /// </summary>
        /// <returns>The mappings.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "get")]
        public IPersistentMap getMappings()
        {
            return Mappings;
        }

        /// <summary>
        /// Get the aliases.
        /// </summary>
        /// <returns>A map of aliases.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "get")]
        public IPersistentMap getAliases()
        {
            return Aliases;
        }


        #endregion

        #region ISerializable Members

        [System.Security.SecurityCritical]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(NamespaceSerializationHelper));
            info.AddValue("_name",_name);
        }

        [Serializable]
        class NamespaceSerializationHelper : IObjectReference
        {
#pragma warning disable 649
            Symbol _name;
#pragma warning restore 649

            #region IObjectReference Members

            public object GetRealObject(StreamingContext context)
            {
               return Namespace.findOrCreate(_name);
            }

            #endregion
        }

        #endregion
    }
}
