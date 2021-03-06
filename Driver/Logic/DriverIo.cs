﻿namespace Driver.Logic
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;

    using global::Driver.Logic.Loaders.Interfaces;
    using global::Driver.Utilities;

    using Microsoft.Win32.SafeHandles;

    public class DriverIo : IDriverIo
    {
        /// <summary>
        /// Gets or sets the <see cref="IDriver" />.
        /// </summary>
        public IDriver Driver
        {
            get;
        }

        /// <summary>
        /// Gets or sets the safe file handle.
        /// </summary>
        public SafeFileHandle Handle
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the connected event.
        /// </summary>
        public EventHandler Connected
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the disconnected event.
        /// </summary>
        public EventHandler Disconnected
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the disposed event.
        /// </summary>
        public EventHandler Disposed
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="IDriverIo"/> is connected.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (this.Handle == null)
                {
                    return false;
                }

                return !this.Handle.IsInvalid && !this.Handle.IsClosed;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Driver"/> is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get;
            private set;
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="DriverIo"/> class from being created.
        /// </summary>
        protected DriverIo()
        {
            // DriverIo.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DriverIo"/> class.
        /// </summary>
        /// <param name="Driver">The driver.</param>
        public DriverIo(IDriver Driver)
        {
            this.Driver = Driver;
        }
        
        /// <summary>
        /// Connects this instance to the driver.
        /// </summary>
        /// <exception cref="Exception">The driver has to be loaded before connecting</exception>
        public void Connect()
        {
            if (this.IsConnected)
            {
                this.Disconnect();
            }

            this.Handle = Native.CreateFile(this.Driver.Config.SymbolicLink, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);

            if (this.IsConnected && this.Connected != null)
            {
                try
                {
                    this.Connected.Invoke(this, EventArgs.Empty);
                }
                catch (Exception)
                {
                    // ..
                }
            }
        }

        /// <summary>
        /// Tries to execute a DeviceIoControl request against the driver.
        /// </summary>
        /// <param name="IoCtl">The IO request control code.</param>
        public bool TryIoControl(uint IoCtl)
        {
            var IoDataReceived = 0;

            if (!DeviceIoControl(this.Handle, IoCtl, null, 0, null, 0, ref IoDataReceived, IntPtr.Zero))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to execute a DeviceIoControl request against the driver.
        /// </summary>
        /// <param name="IoCtl">The IO request control code.</param>
        /// <param name="IoData">The IO request data.</param>
        public bool TryIoControl<TInput>(uint IoCtl, TInput IoData)
        {
            return TryIoControl(IoCtl, IoData, Marshal.SizeOf<TInput>());
        }

        /// <summary>
        /// Tries to execute a DeviceIoControl request against the driver.
        /// </summary>
        /// <param name="IoCtl">The IO request control code.</param>
        /// <param name="IoData">The IO request data.</param>
        /// <param name="IoDataSize">The IO request data size.</param>
        public bool TryIoControl<TInput>(uint IoCtl, TInput IoData, int IoDataSize)
        {
            var IoDataReceived = 0;

            if (!DeviceIoControl(this.Handle, IoCtl, IoData, IoDataSize, null, 0, ref IoDataReceived, IntPtr.Zero))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to execute a DeviceIoControl request against the driver.
        /// </summary>
        /// <param name="IoCtl">The IO request control code.</param>
        /// <param name="IoData">The IO request data.</param>
        /// <param name="IoDataSize">The IO request data size.</param>
        public bool TryIoControl<TInput, TOutput>(uint IoCtl, TInput IoData, TOutput IoOutput)
        {
            return TryIoControl(IoCtl, IoData, Marshal.SizeOf<TInput>(), IoOutput, Marshal.SizeOf<TOutput>());
        }

        /// <summary>
        /// Tries to execute a DeviceIoControl request against the driver.
        /// </summary>
        /// <param name="IoCtl">The IO request control code.</param>
        /// <param name="IoData">The IO request data.</param>
        /// <param name="IoDataSize">The IO request data size.</param>
        public bool TryIoControl<TInput, TOutput>(uint IoCtl, TInput IoData, int IoDataSize, TOutput IoOutput, int IoOutputSize)
        {
            var IoDataReceived = 0;

            if (!DeviceIoControl(this.Handle, IoCtl, IoData, IoDataSize, IoOutput, IoOutputSize, ref IoDataReceived, IntPtr.Zero))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Disconnects this instance from the driver.
        /// </summary>
        /// <exception cref="Exception">The driver has to be loaded before disconnecting</exception>
        public void Disconnect()
        {
            if (this.IsConnected)
            {
                this.Handle.Close();

                try
                {
                    this.Disconnected.Invoke(this, EventArgs.Empty);
                }
                catch (Exception)
                {
                    // ..
                }
            }

            this.Handle = null;
        }

        /// <summary>
        /// Exécute les tâches définies par l'application associées
        /// à la libération ou à la redéfinition des ressources non managées.
        /// </summary>
        public void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.IsDisposed = true;

            // ...

            this.Disconnect();

            // ..

            if (this.Disposed != null)
            {
                try
                {
                    this.Disposed.Invoke(this, EventArgs.Empty);
                }
                catch (Exception)
                {
                    // ...
                }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DeviceIoControl(
            SafeFileHandle HDevice,
            uint IoControlCode,
            [MarshalAs(UnmanagedType.AsAny)][In]    object InBuffer,  int NInBufferSize,
            [MarshalAs(UnmanagedType.AsAny)][Out]   object OutBuffer, int NOutBufferSize,
            ref int PBytesReturned,
            IntPtr Overlapped);
    }
}