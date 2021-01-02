        ��  ��                        �� ��     0 	        4   V S _ V E R S I O N _ I N F O     ���               ?                        l   S t r i n g F i l e I n f o   H   0 4 0 9 0 4 b 0   @   C o m p a n y N a m e     T r a v i s   R o b i n s o n   T   F i l e D e s c r i p t i o n     B e n c h m a r k   A p p l i c a t i o n   0   F i l e V e r s i o n     1 . 1 . 0 . 0   <   I n t e r n a l N a m e   B e n c h m a r k . e x e   j #  L e g a l C o p y r i g h t   C o p y r i g h t   ( C )   2 0 1 0   T r a v i s   R o b i n s o n     D   O r i g i n a l F i l e n a m e   B e n c h m a r k . e x e   L   P r o d u c t N a m e     B e n c h m a r k   A p p l i c a t i o n   4   P r o d u c t V e r s i o n   1 . 1 . 0 . 0   D    V a r F i l e I n f o     $    T r a n s l a t i o n     	��      ��,��$'    0 	        
USAGE: benchmark [list]
                 [pid=] [vid=] [ep=] [intf=] [altf=]
                 [read|write|loop] [notestselect]
                 [verify|verifydetail]
                 [retry=] [timeout=] [refresh=] [priority=]
                 [mode=] [buffersize=] [buffercount=] [packetsize=]
                 
Commands:
         list  : Display a list of connected devices before starting. 
                 Select the device to use for the test from the list.
         read  : Read from the device.
         write : Write to the device.
         loop  : [Default] Read and write to the device at the same time.

         notestselect : Skips submitting the control transfers to get/set the
                        test type.  This makes the application compatible
                        with non-benchmark firmwared. Use at your own risk!

         verify       : Verify received data for loop and read tests. Report
                        basic information on data validation errors.
         verifydetail : Same as verify except reports detail information for 
                        each byte that fails validation.
                        
Switches:
         vid        : Vendor id of device. (hex)  (Default=0x0666)
         pid        : Product id of device. (hex) (Default=0x0001)
         retry      : Number of times to retry a transfer that timeout.
                      (Default = 0)
         timeout    : Transfer timeout value. (milliseconds) (Default=5000)
                      The timeout value used for read/write operations. If a
                      transfer times out more than {retry} times, the test 
                      fails and the operation is aborted.
         mode       : Sync|Async (Default=Sync) 
                      Sync uses the libusb-win32 sync transfer functions.
                      Async uses the libusb-win32 asynchronous api.
         buffersize : Transfer test size in bytes. (Default=4096)
                      Increasing this value will generally yield higher
                      transfer rates.
         buffercount: (Async mode only) Number of outstanding transfers on
                      an endpoint (Default=1, Max=10). Increasing this value
                      will generally yield higher transfer rates.
         refresh    : The display refresh interval. (in milliseconds)
                      (Default=1000) This also effect the running status.
         priority   : AboveNormal|BelowNormal|Highest|Lowest|Normal
                      (Default=Normal) The thread priority level to use
                      for the test.
         ep         : The loopback endpoint to use. For example ep=0x01, would
                      read from 0x81 and write to 0x01. (default is to use the
                      (first read/write endpoint(s) in the interface)
         intf       : The interface id the read/write endpoints reside in.
         intf       : The alt interface id the read/write endpoints reside in.
         packetsize : For isochronous use only. Sets the iso packet size.
                      If not specified, the endpoints maximum packet size
                      is used.         
WARNING:
          This program should only be used with USB devices which implement
          one more more "Benchmark" interface(s).  Using this application
          with a USB device it was not designed for can result in permanent
          damage to the device.
          
Examples:

benchmark vid=0x0666 pid=0x0001
benchmark vid=0x4D2 pid=0x162E
benchmark vid=0x4D2 pid=0x162E buffersize=65536
benchmark read vid=0x4D2 pid=0x162E
benchmark vid=0x4D2 pid=0x162E buffercount=3 buffersize=0x2000
