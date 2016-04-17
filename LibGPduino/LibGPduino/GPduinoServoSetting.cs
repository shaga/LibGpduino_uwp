using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibGPduino
{
    class GPduinoServoSetting
    {
        public bool IsFlip { get; set; }

        public byte Offset { get; set; }

        public byte Amplitude { get; set; }
    }
}
