namespace MegaChaos.Services.Chaos
{
    public interface IChaosEffect
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        
        /// <summary>
        /// Etkinin süresi (saniye cinsinden). 
        /// 0 = Anında gerçekleşir (ör. Altın çalma, eşya verme). 
        /// -1 = Kalıcı (bölüm sonuna kadar).
        /// </summary>
        float DefaultDuration { get; } 

        /// <summary>
        /// Etki başladığında bir kez çağrılır.
        /// </summary>
        void OnStart();

        /// <summary>
        /// Etki aktif olduğu sürece her frame çağrılır.
        /// </summary>
        void OnUpdate(float deltaTime);

        /// <summary>
        /// Etkinin süresi bittiğinde çağrılır. Değişiklikleri geri almak (restore) için kullanılır.
        /// </summary>
        void OnEnd();

        /// <summary>
        /// Etki aktif olduğu sürece her GUI frame'inde (ekrana çizim için) çağrılır.
        /// </summary>
        void OnGUI();
    }
}
