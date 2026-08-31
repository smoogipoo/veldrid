using System;
using System.Runtime.CompilerServices;
using System.Text;
using static Veldrid.MetalBindings.ObjectiveCRuntime;

namespace Veldrid.MetalBindings
{
    public unsafe struct ObjCClass
    {
        private static readonly ObjCClass cls_NSObject = new ObjCClass("NSObject");

        public readonly IntPtr NativePtr;
        public static implicit operator IntPtr(ObjCClass c) => c.NativePtr;

        public ObjCClass(string name)
        {
            int byteCount = Encoding.UTF8.GetMaxByteCount(name.Length);
            byte* utf8BytesPtr = stackalloc byte[byteCount];
            fixed (char* namePtr = name)
                Encoding.UTF8.GetBytes(namePtr, name.Length, utf8BytesPtr, byteCount);

            NativePtr = objc_getClass(utf8BytesPtr);
        }

        public static ObjCClass Create(string name, Action<ObjCClass> build)
        {
            int byteCount = Encoding.UTF8.GetMaxByteCount(name.Length);
            byte* utf8BytesPtr = stackalloc byte[byteCount];
            fixed (char* namePtr = name)
                Encoding.UTF8.GetBytes(namePtr, name.Length, utf8BytesPtr, byteCount);

            ObjCClass newClass = objc_allocateClassPair(cls_NSObject, utf8BytesPtr, 0);
            build(newClass);
            objc_registerClassPair(newClass);

            return newClass;
        }

        public IntPtr GetProperty(string propertyName)
        {
            int byteCount = Encoding.UTF8.GetMaxByteCount(propertyName.Length);
            byte* utf8BytesPtr = stackalloc byte[byteCount];
            fixed (char* namePtr = propertyName)
                Encoding.UTF8.GetBytes(namePtr, propertyName.Length, utf8BytesPtr, byteCount);

            return class_getProperty(this, utf8BytesPtr);
        }

        public string Name => MTLUtil.GetUtf8String(class_getName(this));

        public T Alloc<T>() where T : struct
        {
            IntPtr value = IntPtr_objc_msgSend(NativePtr, Selectors.alloc);
            return Unsafe.AsRef<T>(&value);
        }

        public T AllocInit<T>() where T : struct
        {
            IntPtr value = IntPtr_objc_msgSend(NativePtr, Selectors.alloc);
            objc_msgSend(value, Selectors.init);
            return Unsafe.AsRef<T>(&value);
        }

        public ObjectiveCMethod* class_copyMethodList(out uint count)
        {
            return ObjectiveCRuntime.class_copyMethodList(this, out count);
        }
    }
}
