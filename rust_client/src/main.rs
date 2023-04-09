pub mod protos {
    tonic::include_proto!("Protos");
}

use futures::stream::Stream;
use std::time::Duration;
use tokio_stream::StreamExt;

use protos::chat_app_quic_client::ChatAppQuicClient;
use protos::{GlobalMessageRequest, Request};

// echos a given request as an infinite stream
// TODO: figure out what's going on with this lifetime nonsense
fn echo_requests_iter<'a>(request: Request) -> impl Stream<Item = Request> + 'a {

    // repeat it infinitely as a stream
    tokio_stream::iter(1..usize::MAX).map(move |_| request.clone())
}

// prints a received proto message to the console
fn print_message(message: protos::MessageResponse)
{
    // unwraps the optional sender variable to get val, replacing with "Unknown" if not found
    let sender: String = if let Some(x) = message.sender {
        x.val
    } else {
        "Unknown".into()
    };

    // perform actual console write
    println!("{}: {}", sender, message.message);
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {

    // connect grpc client to service
    let mut client = ChatAppQuicClient::connect("http://localhost:5029")
        .await?;

    // example proto message to send
    let request = Request {
        request: Some(protos::request::Request::GlobalMessage(
            GlobalMessageRequest {
                message: "test message".into(),
            },
        )),
    };

    // create an infinite stream that sends said message every two seconds
    let dur = Duration::from_secs(2);
    let in_stream = echo_requests_iter(request).throttle(dur);

    // initiate bidirectional stream session with
    let response = client
        .session_execute(in_stream)
        .await?;

    // get a stream represnting response messages
    let mut resp_stream = response.into_inner();

    // async receive the messages and handle them
    while let Some(received) = resp_stream.next().await {
        let received = received.unwrap();
        match received.response.unwrap()
        {
            protos::response::Response::Message(x) => {
                print_message(x)
            }
        }
    }

    Ok(())
}
