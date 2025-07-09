namespace Tests
{
    public class FRotatorTests : AbstractTest
    {
        public FRotatorTests()
        {
            Describe("FRotator", () =>
            {
                It("should normalize angles", () =>
                {
                    var r = new FRotator(370, -190, 720);
                    r.Normalize();
                    Expect(r.Pitch).ToBe(10f);
                    Expect(r.Yaw).ToBe(170f);
                    Expect(r.Roll).ToBe(0f);
                });

                It("should add and subtract", () =>
                {
                    var a = new FRotator(10, 20, 30);
                    var b = new FRotator(1, 2, 3);
                    var sum = a + b;
                    var diff = a - b;
                    Expect(sum.Pitch).ToBe(11f);
                    Expect(sum.Yaw).ToBe(22f);
                    Expect(sum.Roll).ToBe(33f);
                    Expect(diff.Pitch).ToBe(9f);
                    Expect(diff.Yaw).ToBe(18f);
                    Expect(diff.Roll).ToBe(27f);
                });

                It("should multiply and divide", () =>
                {
                    var r = new FRotator(10, 20, 30);
                    var mul = r * 2f;
                    var div = r / 2f;
                    Expect(mul.Pitch).ToBe(20f);
                    Expect(mul.Yaw).ToBe(40f);
                    Expect(mul.Roll).ToBe(60f);
                    Expect(div.Pitch).ToBe(5f);
                    Expect(div.Yaw).ToBe(10f);
                    Expect(div.Roll).ToBe(15f);
                });

                It("should compare equality", () =>
                {
                    var a = new FRotator(1, 2, 3);
                    var b = new FRotator(1, 2, 3);
                    var c = new FRotator(0, 0, 0);
                    Expect(a == b).ToBe(true);
                    Expect(a != c).ToBe(true);
                });
            });
        }
    }
}
