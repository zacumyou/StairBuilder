namespace StairBuilder {
// One owned press/release gesture. Cancellation never starts a new gesture.
internal sealed class BulldozerGesture {
 internal int Target=-1;
 internal bool Captured=>Target>=0;
 internal void Reset(){Target=-1;}
 internal bool Step(int hit,bool available,bool pressed,bool held,bool released,bool nativeBlocked,out int remove){
  remove=-1;
  if(Captured){
   int original=Target;
   if(!available){Reset();return true;}
   if(released){Reset();if(hit==original)remove=original;}
   else if(!held)Reset();
   return true;
  }
  if(!nativeBlocked&&available&&hit>=0&&pressed){Target=hit;return true;}
  return false;
 }
}
}
