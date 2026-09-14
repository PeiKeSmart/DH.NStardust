using System.Runtime.CompilerServices;

// 本仓库不做程序集强命名签名（未使用 newlife.snk），因此友元程序集不能带 PublicKey，
// 否则会出现 CS0281：授予程序集的公钥与 InternalsVisibleTo 指定的公钥不匹配
[assembly: InternalsVisibleTo("ClientTest")]
