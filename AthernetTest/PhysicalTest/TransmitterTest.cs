using System;
using System.Threading;
using Athernet.PhysicalLayer.Receive.Rx;
using Athernet.PhysicalLayer.Transmit;
using Xunit;

namespace AthernetTest.PhysicalTest
{
    public class TransmitterTest
    {
        [Fact]
        public void InitTransmitter()
        {
            var transmitter = new Transmitter(0)
            {
                BufferLength = 480000
            };
            Assert.Equal(480000, transmitter.BufferLength);
            Assert.Equal(0, transmitter.DeviceNumber);
            Assert.Equal(TransmitState.Idle, transmitter.State);
        }

        [Fact]
        public void PlayTransmitter()
        {
            var ewh = new EventWaitHandle(false, EventResetMode.AutoReset);

            var transmitter = new Transmitter(0)
            {
                BufferLength = 4800000
            };

            var data = new byte[12];
            new Random().NextBytes(data);
            
            transmitter.AddPayload(data);
            transmitter.AddPayload(data);
            transmitter.AddPayload(data);
            transmitter.AddPayload(data);
            transmitter.Complete();

            transmitter.PlayComplete += (sender, args) => ewh.Set();
            ewh.WaitOne();
        }
    }

    public class ReceiverRxTest
    {
        [Fact]
        public void InitReceiverRx()
        {
            var receiverRx = new ReceiverRx(0, 1000);
            Assert.Equal(0, receiverRx.DeviceNumber);
            Assert.Equal(1000, receiverRx.MaxFrameBytes);
        }

        
    }
}
