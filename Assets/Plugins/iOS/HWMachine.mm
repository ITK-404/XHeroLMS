#import <Foundation/Foundation.h>
#import <sys/utsname.h>

extern "C" {
    const char* _GetHWMachine()
    {
        static char machine[256];

        struct utsname systemInfo;
        uname(&systemInfo);

        strcpy(machine, systemInfo.machine);
        return machine;
    }
}
