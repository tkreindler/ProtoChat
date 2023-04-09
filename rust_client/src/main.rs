pub mod protos {
    tonic::include_proto!("Protos");
}

use futures::stream::Stream;
use std::time::Duration;
use tokio_stream::StreamExt;
use tonic::transport::Channel;

use protos::chat_app_quic_client::ChatAppQuicClient;
use protos::{GlobalMessageRequest, Request, Response};

// echos a given request as an infinite stream
// TODO: figure out what's going on with this lifetime nonsense
fn echo_requests_iter<'a>(request: Request) -> impl Stream<Item = Request> + 'a {

    // repeat it infinitely as a stream
    tokio_stream::iter(1..usize::MAX).map(move |_| request.clone())
}

// async fn bidirectional_streaming_echo(client: &mut EchoClient<Channel>, num: usize) {
//     let in_stream = echo_requests_iter().take(num);

//     let response = client
//         .bidirectional_streaming_echo(in_stream)
//         .await
//         .unwrap();

//     let mut resp_stream = response.into_inner();

//     while let Some(received) = resp_stream.next().await {
//         let received = received.unwrap();
//         println!("\treceived message: `{}`", received.message);
//     }
// }

// async fn bidirectional_streaming_echo_throttle(
//     client: &mut ChatAppQuicClient<Channel>,
//     dur: Duration,
// ) {
//     let request = Request {
//         request: Some(protos::request::Request::GlobalMessage(
//             GlobalMessageRequest {
//                 message: Some("test message"),
//             },
//         )),
//     };

//     client.

//     let response = client
//         .bidirectional_streaming_echo(in_stream)
//         .await
//         .unwrap();

//     let mut resp_stream = response.into_inner();

//     while let Some(received) = resp_stream.next().await {
//         let received = received.unwrap();
//         println!("\treceived message: `{}`", received.message);
//     }
// }

fn print_message(message: protos::MessageResponse)
{
    let sender: String = if let Some(x) = message.sender {
        x.val
    } else {
        "Unknown".into()
    };

    println!("{}: {}", sender, message.message);
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut client = ChatAppQuicClient::connect("https://chat.schulaks.mooo.com")
        .await
        .unwrap();

    let request = Request {
        request: Some(protos::request::Request::GlobalMessage(
            GlobalMessageRequest {
                message: "test message".into(),
            },
        )),
    };

    let dur = Duration::from_secs(2);

    let in_stream = echo_requests_iter(request).throttle(dur);

    let response = client
        .session_execute(in_stream)
        .await
        .unwrap();

    let mut resp_stream = response.into_inner();

    while let Some(received) = resp_stream.next().await {
        let received = received.unwrap();
        match received.response.unwrap()
        {
            protos::response::Response::Message(x) => {
                print_message(x)
            }

            _ => {
                println!("Unknown response found")
            }
        }
    }

    // println!("Streaming echo:");
    // streaming_echo(&mut client, 5).await;
    // tokio::time::sleep(Duration::from_secs(1)).await; //do not mess server println functions

    // // Echo stream that sends 17 requests then graceful end that connection
    // println!("\r\nBidirectional stream echo:");
    // bidirectional_streaming_echo(&mut client, 17).await;

    // Echo stream that sends up to `usize::MAX` requests. One request each 2s.
    // Exiting client with CTRL+C demonstrate how to distinguish broken pipe from
    // graceful client disconnection (above example) on the server side.
    // println!("\r\nBidirectional stream echo (kill client with CTLR+C):");
    // bidirectional_streaming_echo_throttle(&mut client, Duration::from_secs(2)).await;

    Ok(())
}
