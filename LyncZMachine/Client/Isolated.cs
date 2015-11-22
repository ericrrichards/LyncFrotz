namespace LyncZMachine.Client {
    using System;

    public sealed class Isolated<T> : IDisposable where T : MarshalByRefObject {
        // http://www.superstarcoders.com/blogs/posts/executing-code-in-a-separate-application-domain-using-c-sharp.aspx
        private AppDomain _domain;
        private readonly T _value;

        public Isolated() {
            _domain = AppDomain.CreateDomain("Isolated:" + Guid.NewGuid(), null, AppDomain.CurrentDomain.SetupInformation);

            var type = typeof(T);

            _value = (T)_domain.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName);
        }

        public void SetData(string key, string value) {
            if (_domain != null) {
                _domain.SetData(key, value);
            }
        }

        public T Value {
            get {
                return _value;
            }
        }

        public void Dispose() {
            if (_domain == null) {
                return;
            }
            AppDomain.Unload(_domain);
            _domain = null;
        }
    }
}