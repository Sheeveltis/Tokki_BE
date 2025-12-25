namespace Tokki.Domain.Enums
{
    public enum ChatMessageType
    {
        //Tin nhắn thường 
        Text = 0,       
        //Ảnh
        Image = 1,      
        //File
        File = 2,     
        //Audio
        Audio = 3,    
        //TIn nhắn hệ thống ví dụ kiểu Fuwy đã tham gia phòng chat
        System = 9     
    }
}