//
//          Copyright Torben van Hees 2023
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file LICENSE.txt or copy at
//          https://www.boost.org/LICENSE_1_0.txt)
//

using System;

using System.Collections.ObjectModel;
using System.Linq;
using ASCOM.Utilities;
using HidApiAdapter;

using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Reflection;
using ASCOM;
using System.Text;
using System.Threading.Tasks;
using ASCOM.DeviceInterface;
using ASCOM.Utilities.Exceptions;
using System.Threading;

public class NoSuchDeviceException : Exception
{
    public NoSuchDeviceException()
    {
    }

    public NoSuchDeviceException(string message)
        : base(message)
    {
    }

    public NoSuchDeviceException(string message, Exception inner)
        : base(message, inner)
    {
    }

}


public class DimmerNotConnectedException : Exception
{
    public DimmerNotConnectedException()
    {
    }

    public DimmerNotConnectedException(string message)
            : base(message)
    {
    }

    public DimmerNotConnectedException(string message, Exception inner)
            : base(message, inner)
    {
    }
}



public class USBDimmer : IDisposable
{
    const int USBD_VID = 0x04d8;
    const int USBD_PID = 0xf5d1;
    const byte CMD_SETSTATE = 0x10;
    const byte CMD_GETSTATE = 0x11;
    const byte CMD_SETINTEN = 0x20;
    const byte CMD_GETINTEN = 0x21;
    const int USBD_SUCCESS = 0;
    const int USBD_NOT_CONNECTED = -1;
    const int USBD_COMM_ERROR = -2;
    const int MAX_BRIGHTNESS = 1023;

    private HidDevice dimmer;

    public bool TurnOff { get; set; }

    public CalibratorStatus calibratorState { get; private set; }

    public int get_brightness()
    {
        if (!dimmer.IsConnected)
        {
            throw new DimmerNotConnectedException("Spike-A-Flat not connected");
        }
        int inten = 0;
        int ret = 0;
        byte[] rbuf = new byte[4];

        if (Monitor.TryEnter(dimmer, new TimeSpan(0, 0, 3)))
        {
            try
            {

                byte[] wbuf = new byte[4];
                wbuf[0] = CMD_GETINTEN;
                int res = dimmer.Write(wbuf);
                if (res < 0)
                {
                    throw new DimmerNotConnectedException("error writing");
                }

                ret = dimmer.Read(rbuf, 5,1000);

            }
            finally
            {
                Monitor.Exit(dimmer);
            }
        }
        else
        {
            throw new DriverException("error connecting");
        }
        
        if (ret < 3)
        {
            throw new DimmerNotConnectedException("error reading");
        }
        inten = rbuf[1] + 256 * rbuf[2];
        return inten;
    }
    public byte get_state()
    {
        byte[] buf = new byte[8];
        buf[0] = CMD_GETSTATE;
        int res = 0;
        if (Monitor.TryEnter(dimmer, new TimeSpan(0, 0, 3)))
        {
            try
            {
                res = dimmer.Write(buf);
            }
            finally
            {
                Monitor.Exit(dimmer);
            }
        }
        else
        {
            throw new DriverException("Error connecting");
        }
        if (res < 0)
        {
            throw new DimmerNotConnectedException("Error writing");
        }

        res = dimmer.Read(buf, 5, 1000);
        if (res <= 0)
        {
            throw new DimmerNotConnectedException("Error reading");
        }
        return buf[1];
    }

    private void set_brightness_sync(int inten)
    {
        if ((inten > 1023) || (inten < 0))
        {
            throw new ASCOM.InvalidValueException();
        }
        int brightness = inten;

        if (!dimmer.IsConnected)
        {
            throw new DimmerNotConnectedException("Spike-A-Flat not connected");
        }
        byte[]
        buf = new byte[8];
        buf[0] = CMD_SETINTEN;
        buf[1] = (byte)(brightness & 255);
        buf[2] = (byte)(brightness >> 8);
        if (Monitor.TryEnter(dimmer, new TimeSpan(0, 0, 3)))
        {
            try
            {
                int res = dimmer.Write(buf);
                if (res < 0)
                {
                    throw new DimmerNotConnectedException("error writing");
                }

                //read Response
                res = dimmer.Read(buf, 5, 1000);
                if (res <= 0)
                {
                    throw new DimmerNotConnectedException("Timeout reading");
                }

                // setstate goto
                buf[0] = CMD_SETSTATE;
                buf[1] = 3;
                res = dimmer.Write(buf);
                if (res < 0)
                {
                    throw new DimmerNotConnectedException("Error writing");
                }

                //read response
                res = dimmer.Read(buf, 5,1000);
                if (res <= 0)
                {
                    throw new DimmerNotConnectedException("Error reading");
                }
            }
            finally
            {
                Monitor.Exit(dimmer);
            }
        }
        else
        {
            throw new DriverException("error connecting");
        }

    }
    public async Task set_brightness(int inten)
    {
        set_brightness_sync(inten);
    }
    private void initialize()
    {
        IEnumerable<HidDevice> devices = HidDeviceManager.GetManager().SearchDevices(USBD_VID, USBD_PID);
        if (!devices.Any())
        {
            throw new NoSuchDeviceException("Device not found");
        }
        dimmer = devices.First();
        TurnOff = false;
    }
    public USBDimmer()
    {
        this.initialize();

    }

    public USBDimmer(bool turnOff)
    {
        this.initialize();
        TurnOff = turnOff;
    }
    public void Dispose()
    {
        if (dimmer != null)
        {
            if (dimmer.IsConnected)
            {
                disconnect();
            }
            dimmer = null;

        }
    }

    public void connect()
    {
        dimmer.Connect();
    }

    public void disconnect()
    {
        if (TurnOff)
        {
            set_brightness_sync(0);
        }
        dimmer.Disconnect();
    }


}
