using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectSound;
using System.Windows.Forms;
using System.Net.Sockets;

namespace WindowsFormsApplication3
{
    public class soundCapture
    {
        
        private Notify myNotify = null;
        public FileStream fsWav = null;
        public  const int iNotifyNum = 16;
        public int iBufferOffset = 0;
        public int iSampleSize = 0;
        public int iNotifySize = 0;
        public int iBufferSize = 0;
        public BinaryWriter mWriter = null;
        public Capture capture = null;
        public CaptureBuffer capturebuffer = null;
        public AutoResetEvent notifyevent = null;
        public Thread notifythread = null;
        public WaveFormat mWavFormat;
        public Socket sck;
        public Device playDev;
        public IntPtr intPtr;
        public BufferDescription buffDiscript = null;
        public SecondaryBuffer secBuffer;
        public MemoryStream memstream = null;
        public IntPtr Intprt
        {
            set
            {
                intPtr = value;
            }
        }


        public void sckSetter(Socket sckSetting)
        {
            this.sck = sckSetting;
        }
        
        public soundCapture()
        {
            //InitCaptureDevice();
            Console.Write("Set a new soundCapture object");
            mWavFormat = SetWaveFormat();

        }

        public void CreateSecondaryBuffer()
        {
            buffDiscript = new BufferDescription();
            mWavFormat = SetWaveFormat();
            buffDiscript.Format = mWavFormat;
            iNotifySize = mWavFormat.AverageBytesPerSecond / iNotifyNum;
            iBufferSize = iNotifyNum * iNotifySize;
            buffDiscript.BufferBytes = iBufferSize;
            buffDiscript.ControlPan = true;
            buffDiscript.ControlFrequency = true;
            buffDiscript.ControlVolume = true;
            buffDiscript.GlobalFocus = true;
            secBuffer = new SecondaryBuffer(buffDiscript, playDev);
            byte[] bytMemory = new byte[100000];
            memstream = new MemoryStream(bytMemory, 0, 10000, true, true);
            Console.WriteLine("Create a secondarybuffer successfully...");
        }
        

        public void CreateWaveFile(string strFileNName)
        {
            fsWav = new FileStream(strFileNName, FileMode.CreateNew);
            mWriter = new BinaryWriter(fsWav);

            char[] chunkRiff = { 'R', 'I', 'F', 'F' };
            char[] chunkType = { 'W', 'A', 'V', 'E' };
            char[] chunkFmt = { 'f', 'm', 't', ' ' };
            char[] chunkData = { 'd', 'a', 't', 'a' };
            short shPad = 1;
            int nFormatChunkLength = 0x10;
            int nLength = 0;
            short shBytesPerSample = 0;
            if (8 == mWavFormat.BitsPerSample && 1 == mWavFormat.Channels)
                shBytesPerSample = 1;
            else if ((8 == mWavFormat.BitsPerSample && 2 == mWavFormat.Channels) || (16 == mWavFormat.BitsPerSample && 1 == mWavFormat.Channels))
                shBytesPerSample = 2;
            else if (16 == mWavFormat.BitsPerSample && 2 == mWavFormat.Channels)
                shBytesPerSample = 4;

            mWriter.Write(chunkRiff);
            mWriter.Write(nLength);
            mWriter.Write(chunkType);
            mWriter.Write(chunkFmt);
            mWriter.Write(nFormatChunkLength);
            mWriter.Write(shPad);
            mWriter.Write(mWavFormat.Channels);
            mWriter.Write(mWavFormat.SamplesPerSecond);
            mWriter.Write(mWavFormat.AverageBytesPerSecond);
            mWriter.Write(shBytesPerSample);
            mWriter.Write(mWavFormat.BitsPerSample);
            mWriter.Write(chunkData);
            mWriter.Write((int)0);
            Console.WriteLine("file create successfully");

        }

        public bool CreateCaptureDevice()
        {
            CaptureDevicesCollection capturedev = new CaptureDevicesCollection();
            Guid devguid;
            if (capturedev.Count > 0) 
            {
                devguid = capturedev[0].DriverGuid;
                Console.WriteLine("there is an available device...");
            }
            else
            {
                Console.WriteLine("No available device...");
                return false;
            }
            capture = new Capture(devguid);
            return true;
        }

        public void CreateCaptureBuffer()
        {
            CaptureBufferDescription bufferdescription = new CaptureBufferDescription();
            if (null != myNotify)
            {
                myNotify.Dispose();
                myNotify = null;
            }
            if(null != capturebuffer)
            {
                capturebuffer.Dispose();
                capturebuffer = null;
            }
            
            iNotifySize = (1024 > mWavFormat.AverageBytesPerSecond / 8) ? 1024 : (mWavFormat.AverageBytesPerSecond / 8);
            iNotifySize -= iNotifySize % mWavFormat.BlockAlign;
            iBufferSize = iNotifyNum * iNotifySize;
            bufferdescription.Format = mWavFormat;
            bufferdescription.BufferBytes = iBufferSize;
            capturebuffer = new CaptureBuffer(bufferdescription, capture);
            iBufferOffset = 0;
            Console.WriteLine("Create a capture buffer successfully...");
        }

        public void CreateNotification()
        {
            BufferPositionNotify[] bgn = new BufferPositionNotify[iNotifyNum];
            notifyevent = new AutoResetEvent(false);
            notifythread = new Thread(RecoData);
            notifythread.IsBackground = true;
            notifythread.Start();
            for(int i = 0; i < iNotifyNum; i++)
            {
                bgn[i].Offset = iNotifySize + i * iNotifySize - 1;
                bgn[i].EventNotifyHandle = notifyevent.Handle;
            }
            myNotify = new Notify(capturebuffer);
            myNotify.SetNotificationPositions(bgn);
            Console.WriteLine("Create notification successfully...");
        }

        public void RecoData()
        {
            Console.WriteLine("Start to record...");
            while (true)
            {
                notifyevent.WaitOne(Timeout.Infinite, true);
                RecordCapturedData();
                Console.WriteLine("Keep recording...");
            }
        }

        public void RecordCapturedData()
        {
            byte[] capturedata = null;
            int readpos = 0, capturepos = 0, locksize = 0;
            capturebuffer.GetCurrentPosition(out capturepos, out readpos);
            locksize = readpos - iBufferOffset;
            if(locksize < 0)
            {
                locksize += iBufferSize;
            }
            locksize -= (locksize % iNotifySize);
            if (0 == locksize)
                return;
            capturedata = (byte[])capturebuffer.Read(iBufferOffset, typeof(byte), LockFlag.None, locksize);
            sck.Send(capturedata);
            Console.WriteLine("Keep sending data...");
            mWriter.Write(capturedata, 0, capturedata.Length);
            iSampleSize += capturedata.Length;
            iBufferOffset += capturedata.Length;
            iBufferOffset %= iBufferSize;
            Console.WriteLine("Keep writing data into file");
        }

        public void StartRecord(string str)
        {
    
            CreateWaveFile(str);
            CreateCaptureDevice();
            CreateCaptureBuffer();
            CreateNotification();
            capturebuffer.Start(true);
        }

        

        public void stoprec()
        {
            capturebuffer.Stop();
            if (notifyevent != null)
                notifyevent.Set();
            notifythread.Abort();
            RecordCapturedData();
            mWriter.Seek(4, SeekOrigin.Begin);
            mWriter.Write((int)(iSampleSize + 36));
            mWriter.Seek(40, SeekOrigin.Begin);
            mWriter.Write(iSampleSize);
            mWriter.Close();
            fsWav.Close();
            mWriter = null;
            fsWav = null;
        }

        public WaveFormat SetWaveFormat()
        {
            WaveFormat format = new WaveFormat();
            format.FormatTag = WaveFormatTag.Pcm;
            format.SamplesPerSecond = 22050;
            format.BitsPerSample = 16;
            format.Channels = 1;
            format.BlockAlign = (short)(format.Channels * (format.BitsPerSample / 8));
            Console.WriteLine("setformat successfully.....");
            format.AverageBytesPerSecond = format.BlockAlign * format.SamplesPerSecond;
            return format;
        }
        
        private int intPosWrite = 0, intPosPlay = 0, intNotifySize = 5000;
        //following use for client receiving
        public void getVoiceData(int intRecv, byte[] bytRecv)
        {
            
            if (intPosWrite + intRecv <= memstream.Capacity)
            {
                if ((intPosWrite - intPosPlay >= 0 && intPosWrite - intPosPlay < intNotifySize) || (intPosWrite - intPosPlay < 0 && intPosWrite - intPosPlay + memstream.Capacity < intNotifySize))
                {
                        memstream.Write(bytRecv, 0, intRecv);
                        intPosWrite += intRecv;                   
                }
                else if (intPosWrite - intPosPlay >= 0)
                {
                    buffDiscript.BufferBytes = intPosWrite - intPosPlay;
                    SecondaryBuffer sec = new SecondaryBuffer(buffDiscript, playDev);
                    memstream.Position = intPosPlay;
                    sec.Write(0, memstream, intPosWrite - intPosPlay, LockFlag.FromWriteCursor);
                    sec.Play(0, BufferPlayFlags.Default);
                    memstream.Position = intPosWrite;
                    intPosPlay = intPosWrite;
                }
                else if (intPosWrite - intPosPlay < 0)
                {
                    
                    buffDiscript.BufferBytes = intPosWrite - intPosPlay + memstream.Capacity;
                    SecondaryBuffer sec = new SecondaryBuffer(buffDiscript, playDev);
                    memstream.Position = intPosPlay;
                    sec.Write(0, memstream, memstream.Capacity - intPosPlay, LockFlag.FromWriteCursor);
                    memstream.Position = 0;
                    sec.Write(memstream.Capacity - intPosPlay, memstream, intPosWrite, LockFlag.FromWriteCursor);
                    sec.Play(0, BufferPlayFlags.Default);
                    memstream.Position = intPosWrite;
                    intPosPlay = intPosWrite;
                }
            }
            else
            {
                int irest = memstream.Capacity - intPosWrite;
                memstream.Write(bytRecv, 0, irest);
                memstream.Position = 0;
                memstream.Write(bytRecv, irest, intRecv - irest);
                intPosWrite = intRecv - irest;
            }
        }

        public bool CreatePalyDevice()
        {
            DevicesCollection dc = new DevicesCollection();
            Guid g;
            if (dc.Count > 0)
                g = dc[0].DriverGuid;
            else
                return false;
            playDev = new Device(g);
            playDev.SetCooperativeLevel(intPtr, CooperativeLevel.Normal);
            Console.WriteLine("The play device is ------------------------------------------" + playDev.ToString());
            return true;
        }

    }
}
