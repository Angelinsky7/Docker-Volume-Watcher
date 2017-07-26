using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ch.darkink.docker_volume_watcher.trayapp.Helpers {

    public class IconAnimation : IDisposable, INotifyPropertyChanged {

        private Icon m_Current;
        public Icon Current {
            get { return m_Current; }
            private set {
                if (m_Current != value) {
                    m_Current = value;
                    OnPropertyChanged(nameof(Current));
                }
            }
        }

        public Boolean Reverse { get; set; }

        private Bitmap m_Bmp;
        private IList<Icon> m_Icons;
        private Timer m_Timer;
        private Int32 m_Tick;
        private Int32 m_Height;
        private Int32 m_Width;
        private Int32 m_Speed;
        
        public IconAnimation(Bitmap bmp, Int32 height, Int32 width, Int32 speed = 150) {
            m_Bmp = bmp;
            m_Height = height;
            m_Width = width;
            m_Speed = speed;
            
            Color backColor = m_Bmp.GetPixel(1, 1);
            m_Bmp.MakeTransparent(backColor);
            m_Icons = BuildIcons();
        }

        public void Start() {
            Stop();

            m_Timer = new Timer() {
                Interval = m_Speed
            };
            if (Reverse) {
                m_Tick = m_Icons.Count - 1;
                m_Timer.Elapsed += M_Timer_ElapsedReversed;
            } else {
                m_Tick = 0;
                m_Timer.Elapsed += M_Timer_Elapsed;
            }
            m_Timer.Start();
        }

        public void Stop() {
            if (m_Timer != null) {
                m_Timer.Stop();
                m_Timer.Elapsed -= M_Timer_ElapsedReversed;
                m_Timer.Elapsed -= M_Timer_Elapsed;
                m_Timer.Dispose();
                m_Timer = null;
            }
        }

        public Icon GetIcon(Int32 i) {
            return m_Icons[i];
        }

        private void M_Timer_Elapsed(object sender, ElapsedEventArgs e) {
            if (m_Icons != null) {
                Current = m_Icons[m_Tick++];
                if (m_Tick >= m_Icons.Count) { m_Tick = 0; }
            } else {
                Current = null;
            }
        }
        private void M_Timer_ElapsedReversed(object sender, ElapsedEventArgs e) {
            if (m_Icons != null) {
                Current = m_Icons[m_Tick--];
                if (m_Tick < 0) { m_Tick = m_Icons.Count - 1; }
            } else {
                Current = null;
            }
        }

        private IList<Icon> BuildIcons() {
            List<Icon> r = new List<Icon>();

            for (int i = 0; i < (m_Bmp.Width / m_Width); i++) {
                Rectangle rect = new Rectangle(i * m_Width, 0, m_Width, m_Height);
                Bitmap bmp = m_Bmp.Clone(rect, m_Bmp.PixelFormat);
                r.Add(Icon.FromHandle(bmp.GetHicon()));
            }

            return r;
        }

        public void Dispose() {
            Stop();
            if (m_Bmp != null) {
                m_Bmp.Dispose();
                m_Bmp = null;
            }
            if (m_Icons != null) {
                foreach (Icon icon in m_Icons) {
                    icon.Dispose();
                }
                m_Icons.Clear();
                m_Icons = null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(String propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
