using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AthernetTest.UtilsTest
{
    public class MathTest
    {
        [Fact]
        public void GetMostSignificantBitMask()
        {
            Assert.Equal(128, Athernet.Utils.Maths.MostSignificantBitMask(128));
        }
    }
}
